// SPDX-License-Identifier: MPL-2.0

module mizuki;

import std;

bool isInitialized = false;

fn int initialize() @export
{
	if (isInitialized) return 0;

	BackingVersion v = ffms::getVersion();

	io::printfn("FFMS2 Version: %d.%d.%d", v.major, v.minor, v.patch);

	// ffms::initialize();

	isInitialized = true;
	return 0;
}

fn void main()
{
	initialize();
}
