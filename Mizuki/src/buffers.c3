// SPDX-License-Identifier: MPL-2.0

module mizuki::buffers;

import std::collections;
import std::math;
import mizuki::libass;
import mizuki::ffms;
import c;

<*
Initialize the buffer ring
@param g_ctx : `context to initialize buffers for`
@param buffer_count : `number of buffers to start with`
@param max_cache_size_mb : `maximum cache size in megabytes`
@param width : `width of the frame in pixels`
@param height : `height of the frame in pixels`
@param pitch : `frame pitch`

@require pitch > 0 : `frame must have a non-zero pitch`
@require width > 0, height > 0 : `frame must have a non-zero size`
@require buffer_count > 0 : `must have at least 1 initial buffer`
@require max_cache_size_mb >= width * pitch * buffer_count : `max cache size must be big enough to store the initial buffers`
@ensure g_ctx.buffers.frame_buffers.size == buffer_count : `failed to initialize buffer ring`
*>
fn void init_video_buffers(GlobalContext* g_ctx, int buffer_count, int max_cache_size_mb, int width, int height, int pitch)
{
	BuffersContext* ctx = &g_ctx.buffers;
	ctx.frame_buffers.init(mem);
	ctx.max_size = max_cache_size_mb * (long)math::pow(1024, 2);
	for (int i = 0; i < buffer_count; i++)
	{
		ctx.frame_buffers.push(allocate_video_frame(width, height, pitch));
	}
}

<*
Initialize the audio buffer
@param g_ctx : `context to initialize buffer for`
@require g_ctx.ffms.has_audio : `file must have audio`
*>
fn void init_audio_buffer(GlobalContext* g_ctx)
{
	BuffersContext* ctx = &g_ctx.buffers;
	FfmsContext* ffms_ctx = &g_ctx.ffms;

	sz total_samples = (sz)(ffms_ctx.sample_count * ffms_ctx.channel_count);
	short[] buffer_data = mem::alloc_array(short, total_samples);
	ctx.audio_buffer = buffer_data;

	ctx.audio_frame = mem::new(AudioFrame, (AudioFrame) {
		.data = ctx.audio_buffer.ptr,
		.length = total_samples,
		.channel_count = ffms_ctx.channel_count,
		.sample_count = ffms_ctx.sample_count,
		.sample_rate = ffms_ctx.sample_rate,
		.duration_ms = (ffms_ctx.sample_count * 1000) / ffms_ctx.sample_rate,
		.is_valid = false,
	});
}

<*
Free the audio buffer. Used to facilitate audio track switching
@param g_ctx : `context to free the buffer in`
*>
fn void free_audio_buffer(GlobalContext* g_ctx)
{
	BuffersContext* ctx = &g_ctx.buffers;

	if (ctx.audio_frame) mem::free(ctx.audio_frame);
	if (ctx.audio_buffer) mem::free(ctx.audio_buffer);
}

<*
Initialize the visualization buffer
@param g_ctx : `context to initialize buffers for`
*>
fn void init_visualization_buffers(GlobalContext* g_ctx)
{
	BuffersContext* ctx = &g_ctx.buffers;
	ctx.viz_buffers.init(mem);
}

fn FrameGroup* proc_video_frame(GlobalContext* g_ctx, int frame_number, long timestamp)
{
	BuffersContext* ctx = &g_ctx.buffers;
	List{FrameGroup*} buffers = ctx.frame_buffers;

	foreach (sz idx, FrameGroup* buffer : buffers)
	{
		if (buffer.video_frame.is_valid && buffer.video_frame.frame_number == frame_number)
		{
			// If the subtitles need to be re-rendered, the frame needs to be modifiable
			if (!libass::validate_hash(g_ctx, buffer.subtitle_frame))
			{
				// The frame is modifiable
				if (buffer.ref_count == 0)
				{
					// TODO: libass get frame
				}
				else
				{
					// Frame is still in use, find another one
					continue;
				}
			}
			
			// Move buffer to the front of the list
			if (idx != 0)
			{
				buffers.remove_at(idx);
				buffers.push_front(buffer);
			}
			return buffer;
		}
	}
	// TODO
	return null;
}

<*
Allocates a new video frame group on the heap
@param width : `width of the frame in pixels`
@param height : `height of the frame in pixels`
@param pitch : `frame pitch`
@return `pointer to the created frame group`
*>
fn FrameGroup* allocate_video_frame(int width, int height, int pitch)
{
	int total_bytes = height * pitch;
	char[] vid_pixel_buffer = mem::alloc_array(char, total_bytes);
	char[] sub_pixel_buffer = mem::alloc_array(char, total_bytes);

	VideoFrame* vid_frame = mem::new(VideoFrame, (VideoFrame) {
		.frame_number = -1,
		.timestamp = 0,
		.width = width,
		.height = height,
		.pitch = pitch,
		.is_flipped = false,
		.data = vid_pixel_buffer.ptr,
		.is_valid = false,
	});

	SubtitleFrame* sub_frame = mem::new(SubtitleFrame, (SubtitleFrame) {
		.hash = 0,
		.timestamp = 0,
		.width = width,
		.height = height,
		.pitch = pitch,
		.is_flipped = false,
		.data = sub_pixel_buffer.ptr,
		.is_valid = false,
	});

	FrameGroup* group = mem::new(FrameGroup, (FrameGroup) {
		.video_frame = vid_frame,
		.subtitle_frame = sub_frame,
		.ref_count = 0,
	});

	return group;
}
