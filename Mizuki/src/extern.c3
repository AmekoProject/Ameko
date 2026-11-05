// SPDX-License-Identifier: MPL-2.0

module c::ffms;

// Methods
extern fn int get_version() @cname("FFMS_GetVersion");
extern fn void init(int, int) @cname("FFMS_INIT");

// Opaque structs
struct VideoSource { void* _opaque; }
struct AudioSource { void* _opaque; }
struct Indexer { void* _opaque; }
struct Index { void* _opaque; }
struct Track { void* _opaque; }

module c::libass;

// Opaque structs
struct Renderer { void* _opaque; }
struct Library { void* _opaque; }
struct Track { void* _opaque; }