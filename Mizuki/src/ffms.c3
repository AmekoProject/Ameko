// SPDX-License-Identifier: MPL-2.0

module mizuki::ffms;

import std::collections;
import c;

excuse { FILE_NOT_FOUND, INDEXING_FAILED, NO_VIDEO_TRACKS, NO_AUDIO_TRACKS, TRACK_LOADING_FAILED, FRAME_DECODING_FAILED, FRAME_INFO_FAILED, SET_OUTPUT_FORMAT_FAILED, GET_TIME_BASE_FAILED, }bool is_initialized = false;

char[1024] error_buffer;
ffms::ErrorInfo error_info = {
	.type = UNKNOWN,
	.subtype = UNKNOWN,
	.buffer_size = (CInt)error_buffer.len,
	.buffer = (ZString)&error_buffer[0]
};

<* Get the FFMS2 version *>
fn BackingVersion get_version()
{
	int version = c::ffms::get_version();
	return {
		.major = (version >> 24) & 0xFF,
		.minor = (version >> 16) & 0xFF,
		.patch = version & 0xFFFF,
	};
}

<* Initialize FFMS2. Does nothing if already initialized *>
fn void initialize()
{
	if (is_initialized) return;

	ffms::init(0, 0);

	is_initialized = true;
}

fn void? load_video(
	GlobalContext* ctx,
	ZString file_name,
	ZString cache_file_name,
	ZString color_matrix,
	ProgressCallback progress_cb
)
{
	ffms::Index* index     = ctx.ffms.index;
	ffms::Indexer* indexer = ffms::create_indexer(file_name, null); // TODO: error info

	if (indexer == null)
	{
		return;
	}

	if (progress_cb != null)
	{
		ffms::set_progress_cb(indexer, progress_cb, null);
	}

	HashMap{CInt, ZString} tracks;
	tracks.init(mem);
	defer tracks.free();

	// get video tracks
	get_tracks_of_type(indexer, VIDEO, &tracks);
	if (tracks.count == 0) return NO_VIDEO_TRACKS~;

	CInt video_track_number = -1;
	if (tracks.count > 1) video_track_number = 0; // TODO: Ask which track to open

	index = ffms::read_index(cache_file_name, &error_info);

	// Check if the index is for this file
	if (index != null && !ffms::success(ffms::index_belongs_to_file(index, file_name, null)))
	{
		index = null;
	}

	// Check if the track we're looking for is indexed
	if (index != null && video_track_number >= 0)
	{
		ffms::Track* temp_video_track = ffms::get_track_from_index(index, video_track_number);
		if (ffms::get_num_frames(temp_video_track) <= 0) index = null;
	}

	// If we still don't have an index, index now
	if (index == null)
	{
		ffms::set_track_type_index_settings(indexer, VIDEO, 1, 0);
		index = ffms::do_indexing(indexer, ABORT, &error_info);

		if (index == null)
		{
			// TODO: indexing failed!
			// log error
			return INDEXING_FAILED~;
		}

		if (!ffms::success(ffms::write_index(cache_file_name, index, &error_info)))
		{
			// TODO: Log (do not return excuse)
		}
	}
	else
	{
		ffms::cancel_indexing(indexer);
	}

	// If no track number has been selected, use the first one
	if (video_track_number < 0)
	{
		video_track_number = ffms::get_first_indexed_track_of_type(index, VIDEO, &error_info);
		if (video_track_number < 0) return NO_VIDEO_TRACKS~;
	}

	ctx.ffms.video_source = ffms::create_video_source(file_name, video_track_number, index, -1, NORMAL, &error_info);

	// Sourcing failed
	if (ctx.ffms.video_source == null)
	{
		// TODO: log
		return TRACK_LOADING_FAILED~;
	}

	// Fetch video properties
	ffms::VideoProperties* props = ffms::get_video_properties(ctx.ffms.video_source);
	ffms::Frame* frame0          = ffms::get_frame(ctx.ffms.video_source, 0, &error_info);

	if (frame0 == null) return FRAME_DECODING_FAILED~;

	double width  = (double)frame0.encoded_width;
	double height = (double)frame0.encoded_height;
	double dar    = (props.sar_numerator > 0
		&& props.sar_denominator > 0) ? (width * props.sar_numerator) / (height * props.sar_denominator)
		: width / height;

	ctx.ffms.video_color_space = frame0.color_space;
	ctx.ffms.video_color_range = frame0.color_range;
	set_color_space(color_matrix);

	CInt[2] target_formats = { ffms::get_pixel_format("bgra"), -1 };
	if (!ffms::success(
		ffms::set_output_format(
			ctx.ffms.video_source,
			&target_formats[0],
			frame0.encoded_width,
			frame0.encoded_height,
			BICUBIC,
			&error_info
		)
	)) return SET_OUTPUT_FORMAT_FAILED~;

	// Now that we have everything set up, re-fetch the frame
	frame0 = ffms::get_frame(ctx.ffms.video_source, 0, &error_info);
	ctx.ffms.frame_width = (usz)frame0.encoded_width;
	ctx.ffms.frame_height = (usz)frame0.encoded_height;
	ctx.ffms.frame_pitch = (usz)frame0.line_size[0];

	// Additional frame information
	ffms::Track* track = ffms::get_track_from_video(ctx.ffms.video_source);
	if (track == null) return TRACK_LOADING_FAILED~;

	ffms::TrackTimeBase* time_base = ffms::get_time_base(track);
	if (time_base == null) return GET_TIME_BASE_FAILED~;

	// Build lists of timecodes and frames
	ctx.ffms.frame_count = (int)props.frame_count;

	@pool() // Skip having to deinit the lists
	{
		List{CInt} keyframes;
		List{long} timecodes;
		List{long} midcodes;
		List{long} kf_timecodes;
		List{long} kf_midcodes;
		List{long} intervals;
		keyframes.init(mem);
		timecodes.init(mem);
		midcodes.init(mem);
		kf_timecodes.init(mem);
		kf_midcodes.init(mem);
		intervals.init(mem);

		for (CInt frame_number = 0; frame_number < props.frame_count; frame_number++)
		{
			ffms::FrameInfo* frame_info = ffms::get_frame_info(track, frame_number);
			if (frame_info == null) return FRAME_INFO_FAILED~;

			double wc_numerator   = (double)(frame_info.pts * time_base.numerator);
			double wc_denominator = (double)time_base.denominator;
			long wallclock_ms     = (long)(wc_numerator / wc_denominator);
			timecodes.push(wallclock_ms);

			if (frame_info.is_keyframe != 0)
			{
				keyframes.push(frame_number);
				kf_timecodes.push(wallclock_ms);
			}
		}

		ctx.ffms.keyframes = keyframes.to_array(mem);
		ctx.ffms.timecodes = timecodes.to_array(mem);
		ctx.ffms.kf_timecodes = kf_timecodes.to_array(mem);

		// Calculate frame midcodes
		for (CInt frame_number = 0; frame_number < props.frame_count; frame_number++)
		{
			CInt frame1 = frame_number;
			CInt frame2 = frame_number + 1;

			if (frame2 >= props.frame_count)
			{
				CInt frame3 = frame1 - 1;
				long ms3    = timecodes[frame3];
				long ms1    = timecodes[frame1];
				midcodes.push(((ms1 - ms3) / 2) + ms1);
			}
			else
			{
				long ms1 = timecodes[frame1];
				long ms2 = timecodes[frame2];
				midcodes.push(((ms2 - ms1) / 2) + ms1);
			}
		}
		ctx.ffms.midcodes = midcodes.to_array(mem);

		// Get the keyframe midcodes
		foreach (kf : keyframes) kf_midcodes.push(midcodes[kf]);
		ctx.ffms.kf_midcodes = kf_midcodes.to_array(mem);

		// Calculate frame intervals
		for (sz i = 0; i + 1 < props.frame_count; i++)
		{
			intervals.push(timecodes[i + 1] - timecodes[i]);
		}

		// Last interval is 0
		intervals.push(0);
		ctx.ffms.frame_intervals = intervals.to_array(mem);
	};

	ffms::destroy_index(index);
	// Log success
}

<*
 Find the tracks of the given type
 @param indexer : "FFMS indexer"
 @param track_type : `The type of track to look for`
 @param map : `Map to add found tracks to`
*>
fn void get_tracks_of_type(ffms::Indexer* indexer, ffms::TrackType track_type, HashMap{CInt, ZString}* map)
{
	map.clear();
	CInt track_count = ffms::get_num_tracks(indexer);

	for (CInt i = 0; i < track_count; i++)
	{
		if (ffms::get_track_type(indexer, i) == track_type)
		{
			map.set(i, ffms::get_codec_name(indexer, i));
		}
	}
}

<* Set the color space *>
fn void set_color_space(ZString color_matrix)
{
	// TODO
}
