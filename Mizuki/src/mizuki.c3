// SPDX-License-Identifier: MPL-2.0

module mizuki;

import std;

bool is_initialized = false;

fn int initialize() @export
{
	if (is_initialized) return 0;

	BackingVersion v = ffms::get_version();

	io::printfn("FFMS2 Version: %d.%d.%d", v.major, v.minor, v.patch);

	// ffms::initialize();

	is_initialized = true;
	return 0;
}

fn void main()
{
	initialize();
}
