// SPDX-License-Identifier: MPL-2.0

module mizuki;
import c;

struct FfmsContext
{
	c::ffms::Index* index;
	c::ffms::VideoSource* videoSource;
	c::ffms::AudioSource* audioSource;
	char[128] colorSpaceBuffer;
	char* colorSpace;
	int videoColorSpace;
	int videoColorRange;

	int[] keyframes;
	long[] timecodes;
	long[] midcodes;
	long[] kfTimecodes;
	long[] kfMidcodes;
	long[] frameIntervals;
	int frameCount;

	usz frameWidth;
	usz frameHeight;
	usz framePitch;

	bool hasAudio;
}
