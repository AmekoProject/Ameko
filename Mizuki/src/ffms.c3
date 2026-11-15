// SPDX-License-Identifier: MPL-2.0

module mizuki::ffms;

import std::collections;
import std::os;
import c;

excuse { FILE_NOT_FOUND, FILE_NOT_SUPPORTED, INDEXING_FAILED, NO_VIDEO_TRACKS, NO_AUDIO_TRACKS, TRACK_LOADING_FAILED, FRAME_DECODING_FAILED, AUDIO_DECODING_FAILED, FRAME_INFO_FAILED, SET_OUTPUT_FORMAT_FAILED, GET_TIME_BASE_FAILED }bool is_initialized = false;

DylibHandle? ffms2_handle;
GetTrackMetadataI? get_track_metadata_fn;

char[1024] error_buffer;
ErrorInfo error_info = {
	.type = UNKNOWN,
	.subtype = UNKNOWN,
	.buffer_size = (CInt)error_buffer.len,
	.buffer = (ZString)&error_buffer[0]
};

<* Get the FFMS2 version *>
fn BackingVersion get_version()
{
	CInt version = c::ffms::get_version();
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

	$switch:
		$case $feat(MACOS):
			ffms2_handle = os::dlopen_any("libffms2.dylib", "ffms2.dylib");
		$case $feat(WIN32):
			ffms2_handle = os::dlopen_any("ffms2.dll", "libffms2.dll");
		$default:
			ffms2_handle = os::dlopen_any("libffms2.so", "ffms2.so");
	$endswitch

	// No dll found
	if (catch ffms2_handle) return;

	get_track_metadata_fn = os::dlsym_try(ffms2_handle, "FFMS_GetTrackMetadataI");
}

<*
 Load a video track
 @param g_ctx : `context`
 @param file_name : `path of the file to load`
 @param cache_file_name : `path of the index cache file`
 @param color_matrix : `color matrix to use`
 @param progress_cb : `progress callback`
*>
fn void? load_video(
	GlobalContext* g_ctx,
	ZString file_name,
	ZString cache_file_name,
	ZString color_matrix,
	ProgressCallback progress_cb
)
{
	FfmsContext* ctx = &g_ctx.ffms;
	Index* index     = ctx.index;
	Indexer* indexer = ffms::create_indexer(file_name, null); // TODO: error info

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
		SourceTrack* temp_video_track = ffms::get_track_from_index(index, video_track_number);
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

	ctx.video_source = ffms::create_video_source(file_name, video_track_number, index, -1, NORMAL, &error_info);

	// Sourcing failed
	if (ctx.video_source == null)
	{
		// TODO: log
		return TRACK_LOADING_FAILED~;
	}

	// Fetch video properties
	VideoProperties* props = ffms::get_video_properties(ctx.video_source);
	Frame* frame0          = ffms::get_frame(ctx.video_source, 0, &error_info);

	if (frame0 == null) return FRAME_DECODING_FAILED~;

	double width  = (double)frame0.encoded_width;
	double height = (double)frame0.encoded_height;
	double dar    = (props.sar_numerator > 0
		&& props.sar_denominator > 0) ? (width * props.sar_numerator) / (height * props.sar_denominator)
		: width / height;

	ctx.video_color_space = frame0.color_space;
	ctx.video_color_range = frame0.color_range;
	set_color_space(color_matrix);

	CInt[2] target_formats = { ffms::get_pixel_format("bgra"), -1 };
	if (!ffms::success(
		ffms::set_video_output_format(
			ctx.video_source,
			&target_formats[0],
			frame0.encoded_width,
			frame0.encoded_height,
			BICUBIC,
			&error_info
		)
	)) return SET_OUTPUT_FORMAT_FAILED~;

	// Now that we have everything set up, re-fetch the frame
	frame0 = ffms::get_frame(ctx.video_source, 0, &error_info);
	ctx.frame_width = (usz)frame0.encoded_width;
	ctx.frame_height = (usz)frame0.encoded_height;
	ctx.frame_pitch = (usz)frame0.line_size[0];

	// Additional frame information
	SourceTrack* track = ffms::get_track_from_video(ctx.video_source);
	if (track == null) return TRACK_LOADING_FAILED~;

	TrackTimeBase* time_base = ffms::get_time_base(track);
	if (time_base == null) return GET_TIME_BASE_FAILED~;

	// Build lists of timecodes and frames
	ctx.frame_count = (int)props.frame_count;

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
			FrameInfo* frame_info = ffms::get_frame_info(track, frame_number);
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

		ctx.keyframes = keyframes.to_array(mem);
		ctx.timecodes = timecodes.to_array(mem);
		ctx.kf_timecodes = kf_timecodes.to_array(mem);

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
		ctx.midcodes = midcodes.to_array(mem);

		// Get the keyframe midcodes
		foreach (kf : keyframes) kf_midcodes.push(midcodes[kf]);
		ctx.kf_midcodes = kf_midcodes.to_array(mem);

		// Calculate frame intervals
		for (sz i = 0; i + 1 < props.frame_count; i++)
		{
			intervals.push(timecodes[i + 1] - timecodes[i]);
		}

		// Last interval is 0
		intervals.push(0);
		ctx.frame_intervals = intervals.to_array(mem);
	};

	ffms::destroy_index(index);
	// Log success
}

<*
 Load an audio track
 @param g_ctx : `context`
 @param file_name : `path of the file to load`
 @param cache_file_name : `path of the index cache file`
 @param track_number : `index of the track to load. -1 for automatic selection.`
*>
fn void? load_audio(GlobalContext* g_ctx, ZString file_name, ZString cache_file_name, CInt track_number)
{
	FfmsContext* ctx = &g_ctx.ffms;
	Index* index     = ctx.index;
	Indexer* indexer = ffms::create_indexer(file_name, &error_info);

	if (indexer == null)
	{
		if (error_info.subtype == ErrorType.FILE_READ)
		{
			// TODO: Log message
			return FILE_NOT_FOUND~;
		}
		else
		{
			return FILE_NOT_SUPPORTED~;
		}
	}

	// If a track number was not specified, look for the first one
	if (track_number == -1)
	{
		HashMap{CInt, ZString} tracks;
		tracks.init(mem);
		defer tracks.free();

		// get audio tracks
		get_tracks_of_type(indexer, AUDIO, &tracks);
		if (tracks.count == 0)
		{
			ctx.has_audio = false;
			return;
		}

		@pool()
		{
			track_number = tracks.keys(mem)[0];
		};
	}

	index = ffms::read_index(cache_file_name, &error_info);
	if (index != null && !ffms::success(ffms::index_belongs_to_file(index, file_name, &error_info)))
	{
		index = null;
	}

	if (index != null && track_number >= 0)
	{
		SourceTrack* track = ffms::get_track_from_index(index, track_number);
		if (track == null || ffms::get_num_frames(track) <= 0) index = null;
	}

	if (index == null)
	{
		ffms::set_track_index_settings(indexer, track_number, 1, 0);

		index = ffms::do_indexing(indexer, ABORT, &error_info);
		if (index != null && !ffms::success(ffms::write_index(cache_file_name, index, &error_info)))
		{
			// TODO: log, but do not return excuse
		}
	}
	else
	{
		ffms::cancel_indexing(indexer);
	}

	if (index == null) return INDEXING_FAILED~;

	ctx.audio_source = ffms::create_audio_source(file_name, track_number, index, FIRST_VIDEO_TRACK, &error_info);
	if (ctx.audio_source == null) return TRACK_LOADING_FAILED~;

	// Get audio properties
	AudioProperties* props = ffms::get_audio_properties(ctx.audio_source);
	ctx.channel_count = props.channel_count;
	ctx.sample_rate = props.sample_rate;
	ctx.sample_count = props.sample_count;

	// Configure the resampler
	ResampleOptions* rs_options = ffms::create_resample_options(ctx.audio_source);
	defer ffms::destroy_resample_options(rs_options);

	rs_options.sample_format = SIGNED_16;
	if (ctx.channel_count > 2) // Downmix to stereo
	{
		rs_options.channel_layout = FRONT_LEFT | FRONT_RIGHT;
		ctx.channel_count = 2;
	}

	if (!ffms::success(ffms::set_audio_output_format(ctx.audio_source, rs_options, &error_info)))
	{
		// TODO log
	}

	ctx.has_audio = true;
	ffms::destroy_index(index);
}

<*
 Close the loaded video (and audio)
 @param g_ctx : `context`
*>
fn void close_video(GlobalContext* g_ctx)
{
	FfmsContext* ctx = &g_ctx.ffms;
	if (ctx.video_source != null)
	{
		ffms::destroy_video_source(ctx.video_source);
		ctx.video_source = null;
	}

	if (ctx.audio_source != null)
	{
		ffms::destroy_audio_source(ctx.audio_source);
		ctx.audio_source = null;
	}

	if (ctx.index != null)
	{
		ffms::destroy_index(ctx.index);
		ctx.index = null;
	}

	if (ctx.keyframes) mem::free(ctx.keyframes);
	if (ctx.timecodes) mem::free(ctx.timecodes);
	if (ctx.midcodes) mem::free(ctx.midcodes);
	if (ctx.kf_timecodes) mem::free(ctx.kf_timecodes);
	if (ctx.kf_midcodes) mem::free(ctx.kf_midcodes);
	if (ctx.frame_intervals) mem::free(ctx.frame_intervals);
	if (ctx.track_info) mem::free(ctx.track_info);
	ctx.frame_count = 0;
}

<*
 Fetch a video frame and write it to the output buffer
 @param g_ctx : `context`
 @param frame_number : `frame number to fetch`
 @param out : `VideoFrame struct receiving the data`
 @require frame_number >= 0, frame_number < g_ctx.ffms.frame_count : `frame number must be in bounds`
*>
fn void? fetch_frame(GlobalContext* g_ctx, CInt frame_number, VideoFrame* out)
{
	FfmsContext* ctx = &g_ctx.ffms;

	Frame* in = ffms::get_frame(ctx.video_source, frame_number, &error_info);
	if (in == null) return FRAME_DECODING_FAILED~;

	sz total_bytes = (sz)(in.line_size[0] * in.encoded_height);
	mem::copy(out.data, in.data[0], total_bytes);
	out.frame_number = frame_number;
	out.is_valid = true;
}

<*
 Fetch audio samples and write them to the output buffer
 @param g_ctx : `context`
 @param [out] buffer : `buffer to write the samples to`
 @param start : `sample to start fetching`
 @param count : `number of samples to fetch`
 @param progress_cb : `progress callback`
 @require start >= 0, count > 0 : `must fetch at least one sample`
*>
fn void? fetch_audio(GlobalContext* g_ctx, short* buffer, long start, long count, ProgressCallback progress_cb)
{
	FfmsContext* ctx = &g_ctx.ffms;

	if (!ctx.has_audio) return;

	long max_chunk_size     = 4096;
	long current_chunk_size = 0;
	long decoded            = 0;

	while (decoded < count)
	{
		current_chunk_size = count - decoded > max_chunk_size ? max_chunk_size : count - decoded;
		long offset = decoded * ctx.channel_count;

		if (!ffms::success(
			ffms::get_audio(ctx.audio_source, buffer + offset, start + decoded, current_chunk_size, &error_info)
		))
		{
			return AUDIO_DECODING_FAILED~;
		}

		if (progress_cb != null) progress_cb(start + decoded, start + count, null);

		decoded += current_chunk_size;
	}
}

fn Array{TrackInfo}? get_audio_track_info(GlobalContext* g_ctx, ZString file_name)
{
	FfmsContext* ctx = &g_ctx.ffms;
	List{TrackInfo} track_list;
	track_list.init(mem);
	defer track_list.free();

	Indexer* indexer = ffms::create_indexer(file_name, &error_info);
	if (indexer == null)
	{
		if (error_info.subtype == ErrorType.FILE_READ)
		{
			// TODO: Log message
			return FILE_NOT_FOUND~;
		}
		else
		{
			return FILE_NOT_SUPPORTED~;
		}
	}

	HashMap{CInt, ZString} tracks;
	tracks.init(mem);
	defer tracks.free();

	// get audio tracks
	get_tracks_of_type(indexer, AUDIO, &tracks);
	if (tracks.count == 0)
	{
		return (Array{TrackInfo}) {
			.length = 0,
			.ptr = null,
		};
	}

	tracks.@each(; track_number, value)
	{
		ZString track_lang;
		ZString track_title;
		ZString codec_name;

		// If the track metadata function is present, we can fetch the language and title
		if (catch get_track_metadata_fn)
		{
			track_lang = null;
			track_title = null;
			codec_name = ffms::get_codec_name(indexer, track_number);
		}
		else
		{
			track_lang = get_track_metadata_fn(indexer, track_number, "language");
			track_title = get_track_metadata_fn(indexer, track_number, "title");
			codec_name = ffms::get_codec_name(indexer, track_number);
		}

		track_list.push((TrackInfo) {
			.index = track_number,
			.codec = codec_name,
			.language = track_lang,
			.title = track_title,
		});
	};

	// Dealloc the array if it's already allocated
	if (ctx.track_info) mem::free(ctx.track_info);

	ctx.track_info = track_list.to_array(mem);
	return (Array{TrackInfo}) {
		.ptr = ctx.track_info,
		.length = (CInt)track_list.size,
	};
}

<*
 Find the tracks of the given type
 @param indexer : "FFMS indexer"
 @param track_type : `The type of track to look for`
 @param map : `Map to add found tracks to`
*>
fn void get_tracks_of_type(Indexer* indexer, TrackType track_type, HashMap{CInt, ZString}* map)
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
