const std = @import("std");

// Add the required dynamic libraries
fn linkLibraries(b: *std.Build, obj: *std.Build.Step.Compile, target: std.Build.ResolvedTarget) void {
    obj.root_module.addIncludePath(b.path("include"));

    if (target.result.os.tag == .windows) {
        obj.root_module.addLibraryPath(b.path("lib"));
        obj.root_module.linkSystemLibrary("ffms2", .{});
        obj.root_module.linkSystemLibrary("ass", .{});
    }

    if (target.result.os.tag == .macos) {
        obj.root_module.addRPath(b.path("@loader_path"));
        obj.install_name = "@rpath/libmizuki.dylib";

        obj.root_module.addObjectFile(b.path("../Packaging/Ameko-Native-Bin/mac-arm64/libffms2.5.dylib"));
        obj.root_module.addObjectFile(b.path("../Packaging/Ameko-Native-Bin/mac-arm64/libass.9.dylib"));
    }

    if (target.result.os.tag == .linux) {
        obj.root_module.linkSystemLibrary("ffms2", .{});
        obj.root_module.linkSystemLibrary("ass", .{});
    }
    obj.root_module.link_libc = true;
    obj.root_module.strip = false;
}

// Although this function looks imperative, note that its job is to
// declaratively construct a build graph that will be executed by an external
// runner.
pub fn build(b: *std.Build) void {
    // Standard target options allows the person running `zig build` to choose
    // what target to build for. Here we do not override the defaults, which
    // means any target is allowed, and the default is native. Other options
    // for restricting supported target set are available.
    const target = b.standardTargetOptions(.{});

    // Standard optimization options allow the person running `zig build` to select
    // between Debug, ReleaseSafe, ReleaseFast, and ReleaseSmall. Here we do not
    // set a preferred release mode, allowing the user to decide how to optimize.
    const optimize = b.standardOptimizeOption(.{});

    // External dependencies
    const known_folders = b.dependency("known_folders", .{}).module("known-folders");

    // This creates a "module", which represents a collection of source files alongside
    // some compilation options, such as optimization mode and linked system libraries.
    // Every executable or library we compile will be based on one or more modules.
    const lib_mod = b.createModule(.{
        // `root_source_file` is the Zig "entry point" of the module. If a module
        // only contains e.g. external object files, you can make this `null`.
        // In this case the main source file is merely a path, however, in more
        // complicated build scripts, this could be a generated file.
        .root_source_file = b.path("src/mizuki.zig"),
        .target = target,
        .optimize = optimize,
    });

    // Now, we will create a static library based on the module we created above.
    // This creates a `std.Build.Step.Compile`, which is the build step responsible
    // for actually invoking the compiler.
    const lib = b.addLibrary(.{ .name = "mizuki", .root_module = lib_mod, .linkage = .dynamic });

    linkLibraries(b, lib, target);
    lib.root_module.addImport("known-folders", known_folders);

    // This declares intent for the library to be installed into the standard
    // location when the user invokes the "install" step (the default step when
    // running `zig build`).
    b.installArtifact(lib);

    // Creates a step for unit testing. This only builds the test executable
    // but does not run it.
    const lib_unit_tests = b.addTest(.{
        .root_module = lib_mod,
    });

    const run_lib_unit_tests = b.addRunArtifact(lib_unit_tests);

    // Similar to creating the run step earlier, this exposes a `test` step to
    // the `zig build --help` menu, providing a way for the user to request
    // running the unit tests.
    const test_step = b.step("test", "Run unit tests");
    test_step.dependOn(&run_lib_unit_tests.step);
}
