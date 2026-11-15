// SPDX-License-Identifier: MPL-2.0

module mizuki::fnv;

const uint PRIME = 0x01000193;
const uint OFFSET = 0x811c9dc5;

<*
Implementation of the Fowler–Noll–Vo 1a hash function
@param data : `string to be hashed`
@param data_len : `length of data`
*>
fn uint fnv1a_32(ZString data, sz data_len)
{
	uint hash = OFFSET;

	for (sz i = 0; i < data_len - 1; i++)
	{
		char c = data[i];
		hash ^= c;
		hash *= PRIME;
	}
	return hash;
}