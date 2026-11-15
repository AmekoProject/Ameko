// SPDX-License-Identifier: MPL-2.0

module mizuki::libass;

import std::collections;
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