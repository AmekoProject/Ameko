// SPDX-License-Identifier: MPL-2.0

module c::ffms;

// Methods
extern fn CInt get_version() @cname("FFMS_GetVersion");
extern fn void init(CInt, CInt) @cname("FFMS_INIT");
extern fn Indexer* create_indexer(ZString file_name, ErrorInfo* error) @cname("FFMS_CreateIndexer");
extern fn void set_progress_cb(
	Indexer* indexer,
	ProgressCallback callback,
	void* _unused
) @cname("FFMS_SetProgressCallback");
extern fn CInt get_num_tracks(Indexer* indexer) @cname("FFMS_GetNumTracksI");
extern fn TrackType get_track_type(Indexer* indexer, CInt track) @cname("FFMS_GetTrackTypeI");
extern fn ZString get_codec_name(Indexer* indexer, CInt track) @cname("FFMS_GetCodecNameI");
extern fn Index* read_index(ZString file_name, ErrorInfo* error) @cname("FFMS_ReadIndex");
extern fn CInt index_belongs_to_file(
	Index* index,
	ZString file_name,
	ErrorInfo* error
) @cname("FFMS_IndexBelongsToFile");
extern fn Track* get_track_from_index(Index* index, CInt track) @cname("FFMS_GetTrackFromIndex");
extern fn CInt get_num_frames(Track* track) @cname("FFMS_GetNumFrames");
extern fn void set_track_type_index_settings(
	Indexer* indexer,
	TrackType type,
	bool index,
	CInt _unused
) @cname("FFMS_TrackTypeIndexSettings");
extern fn Index* do_indexing(
	Indexer* indexer,
	ErrorHandling error_handling,
	ErrorInfo* error
) @cname("FFMS_DoIndexing2");
extern fn CInt write_index(ZString file_name, Index* index, ErrorInfo* error) @cname("FFMS_WriteIndex");
extern fn void cancel_indexing(Indexer* indexer) @cname("FFMS_CancelIndexing");
extern fn CInt get_first_indexed_track_of_type(
	Index* index,
	TrackType type,
	ErrorInfo* error
) @cname("FFMS_GetFirstIndexedTrackOfType");
extern fn VideoSource* create_video_source(
	ZString file_name,
	CInt track,
	Index* index,
	int threads,
	SeekMode seek_mode,
	ErrorInfo* error
) @cname("FFMS_CreateVideoSource");

// Callbacks
alias ProgressCallback = fn CInt(CLong current, CLong total, void* _unused);

// Opaque structs
typedef VideoSource = void*;
typedef AudioSource = void*;
typedef Indexer = void*;
typedef Index = void*;
typedef Track = void*;

// Structs
struct ErrorInfo
{
	ErrorType type;
	ErrorType subtype;
	CInt buffer_size;
	ZString buffer;
}

// Enums
constset TrackType
{
	UNKNOWN = -1,
	VIDEO,
	AUDIO,
	DATA,
	SUBTITLE,
	ATTACHMENT,
}

constset ErrorType
{
	SUCCESS = 0,

	// Primary types
	INDEX = 1,
	INDEXING,
	POSTPROCESSING,
	SCALING,
	DECODING,
	SEEKING,
	PARSER,
	TRACK,
	WAVE_WRITER,
	CANCELLED,
	RESAMPLING,

	// Subtypes
	UNKNOWN = 20,
	UNSUPPORTED,
	FILE_READ,
	FILE_WRITE,
	NO_FILE,
	VERSION,
	ALLOCATION_FAILED,
	INVALID_ARGUMENT,
	CODEC,
	NOT_AVAILABLE,
	FILE_MISMATCH,
	USER
}

constset ErrorHandling
{
	ABORT       = 0,
	CLEAR_TRACK = 1,
	STOP_TRACK  = 2,
	IGNORE      = 3,
}

constset SeekMode
{
	LINEAR_NO_RW = -1,
	LINEAR       =  0,
	NORMAL       =  1,
	UNSAFE       =  2,
	AGGRESSIVE   =  3,
}

module c::libass;

// Opaque structs
typedef Renderer = void*;
typedef Library = void*;
