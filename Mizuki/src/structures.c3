// SPDX-License-Identifier: MPL-2.0

module mizuki;

struct Array<Type>
{
	Type* ptr;
	int length;
}

struct BackingVersion
{
	int major;
	int minor;
	int patch;
}

struct TrackInfo
{
	CInt index;
	ZString codec;
	ZString language;
	ZString title;
}

struct AssProfilePoint
{
	CInt frame;
	long timestamp;
	double render_ms;
	CInt image_size;
	CInt image_count;
}

struct VideoFrame
{
	int frame_number;
	long timestamp;
	int width;
	int height;
	int pitch;
	bool is_flipped;
	char* data;
	bool is_valid;
}

struct SubtitleFrame
{
	uint hash;
	long timestamp;
	int width;
	int height;
	int pitch;
	bool is_flipped;
	char* data;
	bool is_valid;
}

struct AudioFrame
{
	short* data;
	long length;
	int channel_count;
	long sample_count;
	int sample_rate;
	long duration_ms;
	bool is_valid;
}

struct FrameGroup
{
	VideoFrame* video_frame;
	SubtitleFrame* subtitle_frame;
	int ref_count;
}

struct Bitmap
{
	int width;
	int height;
	int pitch;
	usz capacity;
	char* data;
	int ref_count;
}

struct SyllableInfo
{
	long duration;
	char[48] text;
}
