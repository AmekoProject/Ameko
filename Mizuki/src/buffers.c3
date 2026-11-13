// SPDX-License-Identifier: MPL-2.0

module mizuki::buffers;

import std::collections;
import std::math;
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
*>
fn void? init_video_buffers(GlobalContext* g_ctx, int buffer_count, int max_cache_size_mb, int width, int height, int pitch)
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
