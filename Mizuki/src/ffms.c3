// SPDX-License-Identifier: MPL-2.0

module mizuki::ffms;
import c;

bool is_initialized = false;

<* Get the FFMS2 version *>
fn BackingVersion get_version()
{
	int version = c::ffms::get_version();
	return {
		.major = (version >> 24) & 0xFF,
		.minor = (version >> 16) & 0xFF,
		.patch = version & 0xFFFF,
	};
}

<* Initialize FFMS2. Does nothing if already initialized *>
fn void initialize()
{
	if (is_initialized) return;

	c::ffms::init(0, 0);
	is_initialized = true;
}