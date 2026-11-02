// SPDX-License-Identifier: MPL-2.0

module c::ffms;

extern fn int getVersion() @cname("FFMS_GetVersion");
extern fn void init(int, int) @cname("FFMS_INIT");

module c::libass;
