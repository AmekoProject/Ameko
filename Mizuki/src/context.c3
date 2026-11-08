// SPDX-License-Identifier: MPL-2.0

module mizuki;

import std::collections;
import c;

struct FfmsContext
{
	c::ffms::Index* index;
	c::ffms::VideoSource* video_source;
	c::ffms::AudioSource* audio_source;
	char[128] colorspace_buffer;
	char color_space;
	CInt video_color_space;
	CInt video_color_range;

	CInt[] keyframes;
	long[] timecodes;
	long[] midcodes;
	long[] kf_timecodes;
	long[] kf_midcodes;
	long[] frame_intervals;
	int frame_count;

	usz frame_width;
	usz frame_height;
	usz frame_pitch;

	bool has_audio;
	TrackInfo[] track_info;
	int channel_count;
	int sample_rate;
	long sample_count;
}

struct LibassContext
{
	c::libass::Renderer renderer;
	c::libass::Track track;
	uint current_hash;

	AssProfilePoint[] profile_points;
	HashSet{ulong} profiler_seen_fonts;
}

struct BuffersContext
{
	long max_size;
	long total_size;
	List{FrameGroup*} frame_buffers;
	short* audio_buffer;
	AudioFrame* audio_frame;
	List{Bitmap*} viz_buffers;
}

struct GlobalContext
{
	FfmsContext ffms;
	LibassContext libass;
	BuffersContext buffers;
}

fn GlobalContext* createContext()
{
	GlobalContext* ctx = mem::new(GlobalContext);
	ctx.buffers.frame_buffers.init(mem);
	ctx.buffers.viz_buffers.init(mem);
	ctx.libass.profiler_seen_fonts.init(mem);
	return ctx;
}

fn void destroyContext(GlobalContext* ctx)
{
	if (!ctx) return;

	foreach (fg : ctx.buffers.frame_buffers) mem::free(fg);
	ctx.buffers.frame_buffers.free();

	foreach (bmp : ctx.buffers.viz_buffers) mem::free(bmp);
	ctx.buffers.viz_buffers.free();

	if (ctx.buffers.audio_frame) mem::free(ctx.buffers.audio_frame);
	if (ctx.buffers.audio_buffer) mem::free(ctx.buffers.audio_buffer);

	ctx.libass.profiler_seen_fonts.free();
	// TODO

	mem::free(ctx);
}
