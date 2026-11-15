// SPDX-License-Identifier: MPL-2.0

module mizuki::libass;

import std::collections;
import std::time;
import std::io;
import mizuki::fnv;
import c;

Library* library;

<* Get the libass version *>
fn BackingVersion get_version()
{
	CInt version = c::libass::get_version();
	return {
		.major = version >> 28,
		.minor = (version >> 20) & 0xFF,
		.patch = (version >> 12) & 0xFF,
	};
}

<* Initialize libass. Does nothing if already initialized *>
fn void initialize()
{
	if (library != null) return;

	library = libass::init_library();
	libass::set_message_cb(library, &on_ass_msg, null);
}

<* Deinitialize libass *>
fn void deinitialize(GlobalContext* g_ctx)
{
	LibassContext* ctx = &g_ctx.libass;
	if (ctx.renderer) libass::destroy_renderer(ctx.renderer);
	if (ctx.track) libass::free_track(ctx.track);
	if (library) libass::deinit_library(library);
	ctx.renderer = null;
	ctx.track = null;
	library = null;
}

<*
 Set the subtitles to be rendered
 @param g_ctx : `context`
 @param data : `subtitle file content`
 @param data_len : `length of data`
 @param code_page : `data code page`
*>
fn void set_subtitles(GlobalContext* g_ctx, ZString data, CInt data_len, ZString code_page)
{
	LibassContext* ctx = &g_ctx.libass;
	if (ctx.track) libass::free_track(ctx.track);

	ctx.track = libass::read_memory(library, data, (sz)data_len, code_page);
	ctx.current_hash = fnv::fnv1a_32(data, data_len);
}

<*
 Configure the renderer
 @param g_ctx : `context`
 @param frame_width : `width to render at`
 @param frame_height : `height to render at`
 @require frame_width > 0, frame_height > 0 : `frame must have a non-zero size`
*>
fn void configure_renderer(GlobalContext* g_ctx, CInt frame_width, CInt frame_height)
{
	LibassContext* ctx = &g_ctx.libass;
	if (ctx.renderer) libass::destroy_renderer(ctx.renderer);

	ctx.renderer = libass::create_renderer(library);
	libass::set_frame_size(ctx.renderer, frame_width, frame_height);
	libass::set_storage_size(ctx.renderer, frame_width, frame_height);
	libass::set_font_scale(ctx.renderer, 1.0);
	libass::set_fonts(ctx.renderer, null, "Sans", AUTODETECT, null, 1);
}

<* Clean up *>
fn void clean_up(GlobalContext* g_ctx)
{
	LibassContext* ctx = &g_ctx.libass;
	if (ctx.renderer) libass::destroy_renderer(ctx.renderer);
	if (ctx.track) libass::free_track(ctx.track);
	if (ctx.profile_points) mem::free(ctx.profile_points);
	if (ctx.profiler_seen_fonts.is_initialized()) ctx.profiler_seen_fonts.free();

	ctx.renderer = null;
	ctx.track = null;
}

<*
 Render subtitles on a transparent frame
 @param g_ctx : `context`
 @param timestamp : `timestamp of the frame to render`
 @param out : `destination frame`
 @require timestamp >= 0 : `timestamp cannot be negative`
*>
fn void? fetch_frame(GlobalContext* g_ctx, long timestamp, SubtitleFrame* out)
{
	LibassContext* ctx = &g_ctx.libass;
	sz buffer_size     = (sz)(out.pitch * out.height);

	// Set the frame's hash
	out.hash = ctx.current_hash;

	// Clear the buffer
	mem::set(out.data, 0, buffer_size);

	if (!ctx.track) return;

	libass::set_frame_size(ctx.renderer, out.width, out.height);
	libass::set_storage_size(ctx.renderer, out.width, out.height);

	Image* img = libass::render_frame(ctx.renderer, ctx.track, (CLongLong)timestamp, 0);

	while (img)
	{
		uint o = 255 - (img.color & 0xFF); // opacity
		uint r = img.color >> 24;
		uint g = (img.color >> 16) & 0xFF;
		uint b = (img.color >> 8) & 0xFF;

		for (int y = 0; y < img.height; y++)
		{
			for (int x = 0; x < img.width; x++)
			{
				int dest_x = x + img.x;
				int dest_y = y + img.y;

				if (dest_x < 0 || dest_x >= out.width || dest_y < 0 || dest_y >= out.height) continue;

				int src_index  = y * img.stride + x;
				int dest_index = (dest_y * out.width + dest_x) * 4;

				uint src_alpha = (uint)img.bitmap[src_index];
				uint k         = src_alpha * o / 255;
				uint inv       = 255 - k;

				char* px = &out.data[dest_index];
				px[0] = (char)((k * b + inv * px[0]) / 255);
				px[1] = (char)((k * g + inv * px[1]) / 255);
				px[2] = (char)((k * r + inv * px[2]) / 255);
				px[3] = (char)(k + inv * px[3] / 255);
			}
		}
		img = img.next;
	}
}

<*
 Profile the subtitles
 @param g_ctx : `context`
 @param from_frame : `frame to start at, inclusive`
 @param to_frame : `frame to end at, exclusive`
 @param frame_width : `width to render at`
 @param frame_height : `height to render at`
 @param progress_cb : `progress callback`
 @require frame_width > 0, frame_height > 0 : `frame must have a non-zero size`
 @require to_frame >= from_frame : `to_frame must be after from_frame`
*>
fn Array{AssProfilePoint} profile_subtitles(
	GlobalContext* g_ctx,
	int from_frame,
	int to_frame,
	int frame_width,
	int frame_height,
	ProgressCallback progress_cb
)
{
	LibassContext* ctx = &g_ctx.libass;

	// Set up a dedicated ass_renderer
	Renderer* renderer = libass::create_renderer(library);
	defer libass::destroy_renderer(renderer);

	libass::set_frame_size(renderer, frame_width, frame_height);
	libass::set_storage_size(renderer, frame_width, frame_height);
	libass::set_font_scale(renderer, 1.0);
	libass::set_fonts(renderer, null, "Sans", AUTODETECT, null, 1);

	if (ctx.profile_points) mem::free(ctx.profile_points);

	List{AssProfilePoint} points;
	points.init(mem);
	defer points.free();

	long progress     = 0;
	long max_progress = (long)(to_frame - from_frame);

	for (CInt frame = from_frame; frame < to_frame; frame++, progress++)
	{
		long timestamp   = g_ctx.ffms.timecodes[frame];
		CInt image_count = 0;
		CInt image_size  = 0;

		Time start_time = time::now();

		Image* img = libass::render_frame(renderer, ctx.track, timestamp, 0);

		Time finish_time  = time::now();
		double elapsed_ms = (long)Time.diff_us(finish_time, start_time).to_nano() / 1000.0;

		while (img)
		{
			image_count++;
			image_size += (img.width * img.height);
			img = img.next;
		}

		points.push(
			(AssProfilePoint) {
				.frame = frame,
				.timestamp = timestamp,
				.render_ms = elapsed_ms,
				.image_size = image_size,
				.image_count = image_count,
			}
		);

		if (progress_cb) progress_cb(progress, max_progress, null);
	}

	ctx.profile_points = points.to_array(mem);
	return (Array{AssProfilePoint}) { .ptr = ctx.profile_points, .length = (CInt)points.len() };
}

<*
 Pre-load fonts from a directory
 @param g_ctx : `context`
 @param dir_name : `directory path`
*>
fn bool preload_font_directory(GlobalContext* g_ctx, ZString dir_name)
{
	LibassContext* ctx = &g_ctx.libass;

	if (!ctx.profiler_seen_fonts.is_initialized()) ctx.profiler_seen_fonts.init(mem);

	PathList? files = path::ls(mem, dir_name.str_view().to_tpath());
	if (catch files) return false; // Invalid or something

	foreach (Path path : files)
	{
		if (!is_font_file(path)) continue;

		uint path_hash = path.hash();
		if (ctx.profiler_seen_fonts.contains(path_hash)) continue;
		ctx.profiler_seen_fonts.add(path_hash);

		char[]? font_data = file::load_path_temp(path);
		if (catch font_data) continue;

		libass::add_font(library, path.zstr_tcopy(), font_data.ptr, font_data.len);
	}

	return true;
}

<*
 Validate a frame's content hash
 @param g_ctx : `context to check in`
 @param frame : `subtitle frame to validate`
 @pure
*>
fn bool validate_hash(GlobalContext* g_ctx, SubtitleFrame* frame)
{
	LibassContext* ctx = &g_ctx.libass;
	return ctx.current_hash != 0 && ctx.current_hash == frame.hash;
}

fn void on_ass_msg(CInt level, ZString format, args..., void* data)
{
	char[1024] buffer;
	CInt len = c::libc::vsnprintf(&buffer, buffer.len, format, args);
	if (len <= 0) return;
	// TODO: log
}

<*
 Check if a path points to a font file
 @param path : `path to check`
 @return `true if the path points to a font file`
*>
fn bool is_font_file(Path path)
{
	const String[] FONT_EXTENSIONS = { "ttf", "otf", "ttc", "otc", "pfa", "pfb" };

	if (!path::is_file(path)) return false;

	String? extension = path.extension();
	if (catch extension) return false;

	foreach(ext : FONT_EXTENSIONS)
	{
		if (extension.compare_to_ignore_case(ext) == 0) return true;
	}
	return false;
}
