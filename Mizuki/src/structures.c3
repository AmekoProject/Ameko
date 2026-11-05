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
	int index;
	char* codec;
	char* language;
	char* title;
}

struct AssProfilePoint
{
	int frame;
	long timestamp;
	double renderMs;
	int imageSize;
	int imageCount;
}

struct VideoFrame
{
	int frameNumber;
	long timestamp;
	int width;
	int height;
	int pitch;
	bool flipped;
	char* data;
	bool isValid;
}

struct SubtitleFrame
{
	uint hash;
	long timestamp;
	int width;
	int height;
	int pitch;
	bool flipped;
	char* data;
	bool isValid;
}

struct AudioFrame
{
	short* data;
	long length;
	int channelCount;
	long sampleCount;
	int sampleRate;
	long durationMs;
	bool isValid;
}

struct FrameGroup
{
	VideoFrame* videoFrame;
	SubtitleFrame* subtitleFrame;
	int refCount;
}

struct Bitmap
{
	int width;
	int height;
	int pitch;
	usz capacity;
	char* data;
	int refCount;
}

struct SyllableInfo
{
	long duration;
	char[48] text;
}
