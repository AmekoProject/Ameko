// SPDX-License-Identifier: MPL-2.0

module mizuki::ffms;

import std::collections;
import c;

excuse { FILE_NOT_FOUND, INDEXING_FAILED, NO_VIDEO_TRACKS, NO_AUDIO_TRACKS, TRACK_LOADING_FAILED, }
bool is_initialized = false;
char[1024] error_buffer;

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

	c::ffms::init(0, 0);
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
	c::ffms::Index* index     = ctx.ffms.index;
	c::ffms::Indexer* indexer = c::ffms::create_indexer(file_name, null); // TODO: error info

	if (indexer == null)
	{
		return;
	}

	if (progress_cb != null)
	{
		c::ffms::set_progress_cb(indexer, progress_cb, null);
	}

	HashMap{CInt, ZString} tracks;
	tracks.init(mem);
	defer tracks.free();

	// get video tracks
	get_tracks_of_type(indexer, VIDEO, &tracks);
	if (tracks.count == 0) return NO_VIDEO_TRACKS~;

	CInt video_track_number = -1;
	if (tracks.count > 1) video_track_number = 0; // TODO: Ask which track to open

	index = c::ffms::read_index(cache_file_name, null); // TODO: error info

	// Check if the index is for this file
	if (index != null && c::ffms::index_belongs_to_file(index, file_name, null) != 0)
	{
		index = null;
	}

	// Check if the track we're looking for is indexed
	if (index != null && video_track_number >= 0)
	{
		c::ffms::Track* temp_video_track = c::ffms::get_track_from_index(index, video_track_number);
		if (c::ffms::get_num_frames(temp_video_track) <= 0) index = null;
	}

	// If we still don't have an index, index now
	if (index == null)
	{
		c::ffms::set_track_type_index_settings(indexer, VIDEO, true, 0);
		index = c::ffms::do_indexing(indexer, ABORT, null); // TODO: error info

		if (index == null)
		{
			// TODO: indexing failed!
			// log error
			return INDEXING_FAILED~;
		}
		CInt write_result = c::ffms::write_index(cache_file_name, index, null); // TODO: error info
		if (write_result != 0)
		{
			// TODO: Log (do not return excuse)
		}
	}
	else
	{
		c::ffms::cancel_indexing(indexer);
	}

	// If no track number has been selected, use the first one
	if (video_track_number < 0)
	{
		video_track_number = c::ffms::get_first_indexed_track_of_type(index, VIDEO, null); // TODO: error info
		if (video_track_number < 0) return NO_VIDEO_TRACKS~;
	}

	ctx.ffms.video_source = c::ffms::create_video_source(
		file_name,
		video_track_number,
		index,
		-1,
		NORMAL,
		null
	); // TODO: error info

	// Sourcing failed
	if (ctx.ffms.video_source == null)
	{
		// TODO: log
		return TRACK_LOADING_FAILED~;
	}
}

<*
 Find the tracks of the given type
 @param indexer : "FFMS indexer"
 @param track_type : `The type of track to look for`
 @param map : `Map to add found tracks to`
*>
fn void get_tracks_of_type(c::ffms::Indexer* indexer, c::ffms::TrackType track_type, HashMap{CInt, ZString}* map)
{
	map.clear();
	CInt track_count = c::ffms::get_num_tracks(indexer);

	for (CInt i = 0; i < track_count; i++)
	{
		if (c::ffms::get_track_type(indexer, i) == track_type)
		{
			map.set(i, c::ffms::get_codec_name(indexer, i));
		}
	}
}
