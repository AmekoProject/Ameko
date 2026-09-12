// SPDX-License-Identifier: MPL-2.0

//! Optional symbol loader

const std = @import("std");
const builtin = @import("builtin");

// Win32 external definitions
extern "kernel32" fn LoadLibraryExW(
    lpLibFileName: [*:0]const u16,
    hFile: ?*anyopaque,
    dwFlags: u32,
) callconv(.winapi) ?std.os.windows.HMODULE;
extern "kernel32" fn GetProcAddress(hModule: std.os.windows.HMODULE, lpProcName: [*:0]const u8) callconv(.winapi) ?std.os.windows.FARPROC;
extern "kernel32" fn FreeLibrary(hLibModule: std.os.windows.HMODULE) callconv(.winapi) i32;
const LOAD_LIBRARY_SEARCH_DEFAULT_DIRS = 0x1000;

const ffms2_candidates: []const [:0]const u8 = switch (builtin.os.tag) {
    .windows => &.{ "ffms2.dll", "libffms2.dll" },
    .macos => &.{ "libffms2.dylib", "ffms2.dylib" },
    .linux => &.{ "libffms2.so", "ffms2.so" },
    else => &.{},
};

pub const Library = struct {
    const Self = @This();
    const Handle = if (builtin.os.tag == .windows) std.os.windows.HMODULE else std.DynLib;

    handle: ?Handle,

    pub fn open(path: [:0]const u8) !Self {
        if (comptime builtin.os.tag == .windows) {
            var path_utf16: [std.fs.max_path_bytes:0]u16 = undefined;
            const n = try std.unicode.utf8ToUtf16Le(&path_utf16, path);
            path_utf16[n] = 0;

            return .{
                .handle = LoadLibraryExW(
                    @ptrCast(path_utf16[0..n].ptr),
                    null,
                    LOAD_LIBRARY_SEARCH_DEFAULT_DIRS,
                ),
            };
        } else {
            return .{
                .handle = try std.DynLib.open(path),
            };
        }
    }

    pub fn close(self: *Self) void {
        if (comptime builtin.os.tag == .windows) {
            FreeLibrary(self.handle.?);
        } else {
            self.handle.?.close();
        }
    }

    pub fn lookup(self: *Self, comptime T: type, name: [:0]const u8) ?T {
        if (comptime builtin.os.tag == .windows) {
            const addr = GetProcAddress(
                self.handle.?,
                name.ptr,
            ) orelse return null;

            return @ptrCast(@alignCast(addr));
        } else {
            return self.handle.?.lookup(T, name);
        }
    }
};

fn OpenFirstAvailable(candidates: []const [:0]const u8) ?Library {
    for (candidates) |candidate| {
        if (Library.open(candidate)) |lib| {
            return lib;
        } else |_| {
            continue;
        }
    }
    return null;
}

pub fn LoadFfms2() ?Library {
    return OpenFirstAvailable(ffms2_candidates);
}
