// SPDX-License-Identifier: MPL-2.0

module c::ffms;

<* Helper method for evaluating successful FFMS API calls *>
fn bool success(CInt result) => result == 0;

// Methods
extern fn void init(CInt, CInt) @cname("FFMS_INIT");
extern fn CInt get_version() @cname("FFMS_GetVersion");
extern fn void destroy_index(Index* index) @cname("FFMS_DestroyIndex");
extern fn CInt get_pixel_format(ZString name) @cname("FFMS_GetPixFmt");
extern fn CInt get_num_tracks(Indexer* indexer) @cname("FFMS_GetNumTracksI");
extern fn CInt get_num_frames(SourceTrack* track) @cname("FFMS_GetNumFrames");
extern fn CInt get_num_tracks(Indexer* indexer) @cname("FFMS_GetNumTracksI");
extern fn void cancel_indexing(Indexer* indexer) @cname("FFMS_CancelIndexing");
extern fn TrackTimeBase* get_time_base(SourceTrack* track) @cname("FFMS_TrackTimeBase");
extern fn Index* read_index(ZString file_name, ErrorInfo* error) @cname("FFMS_ReadIndex");
extern fn void destroy_video_source(VideoSource* source) @cname("FFMS_DestroyVideoSource");
extern fn void destroy_audio_source(AudioSource* source) @cname("FFMS_DestroyAudioSource");
extern fn ZString get_codec_name(Indexer* indexer, CInt track) @cname("FFMS_GetCodecNameI");
extern fn TrackType get_track_type(Indexer* indexer, CInt track) @cname("FFMS_GetTrackTypeI");
extern fn SourceTrack* get_track_from_video(VideoSource* source) @cname("FFMS_GetTrackFromVideo");
extern fn Indexer* create_indexer(ZString file_name, ErrorInfo* error) @cname("FFMS_CreateIndexer");
extern fn Frame* get_frame(VideoSource* source, CInt frame, ErrorInfo* error) @cname("FFMS_GetFrame");
extern fn SourceTrack* get_track_from_index(Index* index, CInt track) @cname("FFMS_GetTrackFromIndex");
extern fn FrameInfo* get_frame_info(SourceTrack* track, CInt frame_number) @cname("FFMS_GetFrameInfo");
extern fn VideoProperties* get_video_properties(VideoSource* source) @cname("FFMS_GetVideoProperties");
extern fn AudioProperties* get_audio_properties(AudioSource* source) @cname("FFMS_GetAudioProperties");
extern fn CInt write_index(ZString file_name, Index* index, ErrorInfo* error) @cname("FFMS_WriteIndex");
extern fn void destroy_resample_options(ResampleOptions* options) @cname("FFMS_DestroyResampleOptions");
extern fn ResampleOptions* create_resample_options(AudioSource* source) @cname("FFMS_CreateResampleOptions");

extern fn CInt get_first_indexed_track_of_type(
	Index* index,
	TrackType type,
	ErrorInfo* error
) @cname("FFMS_GetFirstIndexedTrackOfType");

extern fn void set_track_type_index_settings(
	Indexer* indexer,
	TrackType type,
	CInt shoudld_index,
	CInt _unused
) @cname("FFMS_TrackTypeIndexSettings");

extern fn VideoSource* create_video_source(
	ZString file_name,
	CInt track,
	Index* index,
	CInt threads,
	SeekMode seek_mode,
	ErrorInfo* error
) @cname("FFMS_CreateVideoSource");

extern fn AudioSource* create_audio_source(
	ZString file_name,
	CInt track,
	Index* index,
	AudioDelayMode delay_mode,
	ErrorInfo* error
) @cname("FFMS_CreateAudioSource");

extern fn void set_track_index_settings(
	Indexer* indexer,
	CInt track,
	CInt should_index,
	CInt _unused
) @cname("FFMS_TrackIndexSettings");

extern fn CInt set_video_output_format(
	VideoSource* source,
	CInt* formats,
	CInt width,
	CInt height,
	Resizer resizer,
	ErrorInfo* error
) @cname("FFMS_SetOutputFormatV2");

extern fn CInt set_audio_output_format(
	AudioSource* source,
	ResampleOptions* resample_options,
	ErrorInfo* error
) @cname("FFMS_SetOutputFormatA");

extern fn CInt index_belongs_to_file(
	Index* index,
	ZString file_name,
	ErrorInfo* error
) @cname("FFMS_IndexBelongsToFile");

extern fn void set_progress_cb(
	Indexer* indexer,
	ProgressCallback callback,
	void* _unused
) @cname("FFMS_SetProgressCallback");

extern fn Index* do_indexing(
	Indexer* indexer,
	ErrorHandling error_handling,
	ErrorInfo* error
) @cname("FFMS_DoIndexing2");

extern fn int get_audio(
	AudioSource* source,
	void* buffer,
	long start,
	long count,
	ErrorInfo* error
) @cname("FFMS_GetAudio");

// Optional functions
alias GetTrackMetadataI = fn ZString(Indexer* indexer, CInt track, ZString key);

// Callbacks
alias ProgressCallback = fn CInt(CLong current, CLong total, void* _unused);

// Opaque structs
typedef VideoSource = void*;
typedef AudioSource = void*;
typedef Indexer = void*;
typedef Index = void*;
typedef SourceTrack = void*;

// Structs
struct ErrorInfo
{
	ErrorType type;
	ErrorType subtype;
	CInt buffer_size;
	ZString buffer;
}

struct FrameInfo
{
	long pts;
	CInt is_repeat_pict;
	CInt is_keyframe;
	long original_pts;
}

struct VideoProperties
{
	CInt fps_denominator;
	CInt fps_numerator;
	CInt rff_denominator;
	CInt rff_numerator;
	CInt frame_count;
	CInt sar_numerator;
	CInt sar_denominator;
	// Partial implementation
}

struct AudioProperties
{
	SampleFormat sample_format;
	CInt sample_rate;
	CInt bits_per_sample;
	CInt channel_count;
	AudioChannel channel_layout;
	long sample_count;
	double first_time;
	double last_time;
	double last_end_time;
}

struct ResampleOptions
{
	AudioChannel channel_layout;
	SampleFormat sample_format;
	CInt sample_rate;
	// Partial implementation
}

struct Frame
{
	char*[4] data;
	CInt[4] line_size;
	CInt encoded_width;
	CInt encoded_height;
	CInt encoded_pixel_format;
	CInt scaled_width;
	CInt scaled_height;
	CInt converted_pixel_format;
	CInt is_key_frame;
	CInt is_repeat_pict;
	CInt is_interlaced_frame;
	CInt is_top_field_first;
	char pict_type;
	CInt color_space;
	CInt color_range;
	// Partial implementation
}

struct TrackTimeBase
{
	long numerator;
	long denominator;
}

// Enums
constset TrackType : CInt
{
	UNKNOWN = -1,
	VIDEO,
	AUDIO,
	DATA,
	SUBTITLE,
	ATTACHMENT,
}

constset ErrorType : CInt
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
	USER,
}

constset ErrorHandling : CInt
{
	ABORT       = 0,
	CLEAR_TRACK = 1,
	STOP_TRACK  = 2,
	IGNORE      = 3,
}

constset SeekMode : CInt
{
	LINEAR_NO_RW = -1,
	LINEAR       =  0,
	NORMAL       =  1,
	UNSAFE       =  2,
	AGGRESSIVE   =  3,
}


constset Resizer : CInt
{
	FAST_BILINEAR = 0x0001,
	BILINEAR      = 0x0002,
	BICUBIC       = 0x0004,
	X             = 0x0008,
	POINT         = 0x0010,
	AREA          = 0x0020,
	BICUBLIN      = 0x0040,
	GAUSS         = 0x0080,
	SINC          = 0x0100,
	LANCZOS       = 0x0200,
	SPLINE        = 0x0400,
}

constset AudioDelayMode : CInt
{
	NO_SHIFT          = -3,
	TIME_ZERO         = -2,
	FIRST_VIDEO_TRACK = -1,
}

constset SampleFormat : CInt
{
	UNSIGNED_8 = 0,
	SIGNED_16,
	SIGNED_32,
	FLOAT,
	DOUBLE
}

constset AudioChannel : long
{
	FRONT_LEFT  = 0x00000001,
	FRONT_RIGHT = 0x00000002,
	// Partial implementation
}

module c::libass;

// Opaque structs
typedef Renderer = void*;
typedef Library = void*;
typedef SubtitleTrack = void*;
