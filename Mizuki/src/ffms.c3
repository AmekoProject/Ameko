// SPDX-License-Identifier: MPL-2.0

module mizuki::ffms;
import c;

bool isInitialized = false;

<* Get the FFMS2 version *>
fn BackingVersion getVersion()
{
	int version = c::ffms::getVersion();
	return {
		.major = (version >> 24) & 0xFF,
		.minor = (version >> 16) & 0xFF,
		.patch = version & 0xFFFF,
	};
}

<* Initialize FFMS2. Does nothing if already initialized *>
fn void initialize()
{
	if (isInitialized) return;

	c::ffms::init(0, 0);
	isInitialized = true;
}