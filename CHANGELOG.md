# Changelog

## [0.1.7](https://github.com/adr0ps1/Sabeltann/compare/v0.1.6...v0.1.7) (2026-06-14)


### Features

* Settings window with default volume, auto-load toggle, connection info; cap initial channel display at 500 ([7ed2782](https://github.com/adr0ps1/Sabeltann/commit/7ed2782bc3f7f8bb74573cf126431f8012db45f0))
* Smart channel split (Live vs VOD), transport auto-hide, X stop button ([2bfb263](https://github.com/adr0ps1/Sabeltann/commit/2bfb263952a186606015b5816772b265630a8a87))
* Space to pause/play keyboard shortcut ([1cb1da9](https://github.com/adr0ps1/Sabeltann/commit/1cb1da99494c59b91446d0d915b4418feb78da00))


### Bug Fixes

* Accurate Live vs VOD split using provider group-title data; Movies & Series now shows content ([55862d6](https://github.com/adr0ps1/Sabeltann/commit/55862d64590383e06dcc015bf31809f7ea510b3c))
* ContentPicker spacing, autoplay on category select, group drill-down ([bb0d120](https://github.com/adr0ps1/Sabeltann/commit/bb0d120228fca3ead7576ed2037f3c0c5e21b4db))
* Debug toggle button, crash logging, D-key fix, constructor cleanup ([73e2bfa](https://github.com/adr0ps1/Sabeltann/commit/73e2bfaf10fce55e3f1de985ccd799607b543e6b))
* Debug toggle button, crash logging, D-key no longer conflicts with search ([b8006e0](https://github.com/adr0ps1/Sabeltann/commit/b8006e00dc6c90bd501febf1a6b00e7bc451d9a5))
* Exact group-name matching for Live/VOD split; reconnection progress overlay ([987f819](https://github.com/adr0ps1/Sabeltann/commit/987f819e724e423e711f49f21065fc32def9bb27))
* Fullscreen hides title bar and status bar completely ([a6d9c6a](https://github.com/adr0ps1/Sabeltann/commit/a6d9c6a2d7f54f8034c0de24ced78081d3b8b292))
* Fullscreen hides title bar completely ([2c01acd](https://github.com/adr0ps1/Sabeltann/commit/2c01acdd7ab2f2e21f2b32693cf80425aaf945f3))
* Fullscreen hides top title bar and bottom status bar via row collapsing ([16df6c5](https://github.com/adr0ps1/Sabeltann/commit/16df6c59346f71c36729a30cf03474f7101017a9))
* Fullscreen hides top title bar and bottom status bar via row collapsing ([47bb455](https://github.com/adr0ps1/Sabeltann/commit/47bb455876b13e56e07421c582a7fc1991734ec3))
* Move AGENTS.md to global config, remove from repo ([b91e53a](https://github.com/adr0ps1/Sabeltann/commit/b91e53a642a6f76689f4ef5fa7509a1ee04b57f6))
* Movies & Shows grid with category cards, fix pending playlist re-use, fix back button ([3a57b17](https://github.com/adr0ps1/Sabeltann/commit/3a57b175a61ea5e91adc8b94f161b4182ca7b818))
* Popup transport bar with auto-hide, debug panel always on top, stop exits fullscreen ([b4c1667](https://github.com/adr0ps1/Sabeltann/commit/b4c1667bb76bf71437feb8eaaf443b24a2365e5c))
* Remove CategoryGrid (causes freeze), both buttons show channel list ([69fd61a](https://github.com/adr0ps1/Sabeltann/commit/69fd61a966a580deb355f1786de5acced3969d70))
* Remove Sentry, add XamlLoader for .NET 10 compat, README license notice, search pool fix ([e61a854](https://github.com/adr0ps1/Sabeltann/commit/e61a8543930df8f95e0a51716021f31d8a0d27dc))
* Resolve merge conflicts, clean up constructor ([156bb5c](https://github.com/adr0ps1/Sabeltann/commit/156bb5c9db442369ee62a60a2c79957f46987f42))
* Sentry removal, XamlLoader for .NET 10 compat, search pool, README notice ([f592cbe](https://github.com/adr0ps1/Sabeltann/commit/f592cbe8fb8172e0c14c7a96dffd887d68080b84))
* VLC null-safe, .NET 9 target, error handling ([1d4619a](https://github.com/adr0ps1/Sabeltann/commit/1d4619afdd8d87d54203493f6c8322a6ce0b092c))
* XamlLoader for .NET 10, search pool, Sentry cleanup, Settings window ([391393d](https://github.com/adr0ps1/Sabeltann/commit/391393d0f2fbf0200d71733107f32f300fadb7f9))
* XamlLoader populates fields even if Load fails; ConnectionPage uses FindControl ([cfd23c1](https://github.com/adr0ps1/Sabeltann/commit/cfd23c1906188798da8079758783b56fbd9e5a6e))


### Chores

* Add GPL-3.0 license, code signing policy, privacy policy ([eb20a0c](https://github.com/adr0ps1/Sabeltann/commit/eb20a0c0f507e9aabdfd90bd44bb2107639f956b))

## [0.1.6](https://github.com/adr0ps1/Sabeltann/compare/v0.1.5...v0.1.6) (2026-06-13)


### Bug Fixes

* Replace invalid ICO with proper icon from SVG ([d50cd2c](https://github.com/adr0ps1/Sabeltann/commit/d50cd2c9f88e29d71a594fecfa0250d65100785b))

## [0.1.5](https://github.com/adr0ps1/Sabeltann/compare/v0.1.4...v0.1.5) (2026-06-13)


### Features

* Embed app icon in EXE; add ARPPRODUCTICON and shortcut icons to MSI ([332db5b](https://github.com/adr0ps1/Sabeltann/commit/332db5b8ee6ed392d5fb638ccebc42a6b0176c6f))


### Bug Fixes

* Re-enable Sigstore attestation for public repo ([c482720](https://github.com/adr0ps1/Sabeltann/commit/c482720ac1f383ce7f6cf479f40c17fa710e1d4b))

## [0.1.4](https://github.com/adr0ps1/Sabeltann/compare/v0.1.3...v0.1.4) (2026-06-13)


### Bug Fixes

* Correct action commit SHAs in release workflow ([0d1d65b](https://github.com/adr0ps1/Sabeltann/commit/0d1d65b07d46cfdddf731f349f3e0bc773c9f297))
* Remove sigstore attestation (private repo); use WiX v4 CLI for MSI ([7791753](https://github.com/adr0ps1/Sabeltann/commit/77917530bf78fa1d4e1f7b39243472bfb3657e32))
* Replace invalid wixsharp CLI with WixSharp.bin NuGet package ([79d56c1](https://github.com/adr0ps1/Sabeltann/commit/79d56c185bb641f893d5988b74fb3c842995a62c))
* Replace WixSharp with WiX v4 CLI for MSI installer ([8413341](https://github.com/adr0ps1/Sabeltann/commit/8413341e0a12e79cacb772347e48826564489219))


### Chores

* Add release-please manifest ([2e3e6f4](https://github.com/adr0ps1/Sabeltann/commit/2e3e6f48b40bf26fabba4aa8ff4f44a624efe73e))
* **main:** Release 0.1.1 ([d6b155c](https://github.com/adr0ps1/Sabeltann/commit/d6b155c02c723e9883a41c2747aaaf2183db4ccd))
* **main:** Release 0.1.1 ([d6b155c](https://github.com/adr0ps1/Sabeltann/commit/d6b155c02c723e9883a41c2747aaaf2183db4ccd))
* **main:** Release 0.1.1 ([1f13c05](https://github.com/adr0ps1/Sabeltann/commit/1f13c055e9b1faadfaa577186c4d8f767de26a3a))
* **main:** Release 0.1.2 ([b47a443](https://github.com/adr0ps1/Sabeltann/commit/b47a443580cd5489442a8ec01572857341bf6f38))
* **main:** Release 0.1.2 ([b47a443](https://github.com/adr0ps1/Sabeltann/commit/b47a443580cd5489442a8ec01572857341bf6f38))
* **main:** Release 0.1.2 ([851b20c](https://github.com/adr0ps1/Sabeltann/commit/851b20cb46d72fa01788eab00a98df9d3b91a0e2))
* **main:** Release 0.1.3 ([9bcb09c](https://github.com/adr0ps1/Sabeltann/commit/9bcb09c56b86f56e2e010f079e6d903f9244f324))
* **main:** Release 0.1.3 ([9bcb09c](https://github.com/adr0ps1/Sabeltann/commit/9bcb09c56b86f56e2e010f079e6d903f9244f324))
* **main:** Release 0.1.3 ([1c82dcd](https://github.com/adr0ps1/Sabeltann/commit/1c82dcdbf255583389a35a931d9936946b7978d5))
* Trigger fresh release cycle for v0.1.1 ([fee254d](https://github.com/adr0ps1/Sabeltann/commit/fee254d2ce19179774864708911d9213522ae728))
* Trigger release workflow with permissions fixed ([6750766](https://github.com/adr0ps1/Sabeltann/commit/675076608fc1d27243f668c8e7a4e9fc567c4fcf))


### Documentation

* Add emoji-packed README ([67bf69f](https://github.com/adr0ps1/Sabeltann/commit/67bf69fa8dcd6419b4f89692db4b3d07f9407edf))

## [0.1.3](https://github.com/adr0ps1/Sabeltann/compare/v0.1.2...v0.1.3) (2026-06-13)


### Bug Fixes

* Replace WixSharp with WiX v4 CLI for MSI installer ([8413341](https://github.com/adr0ps1/Sabeltann/commit/8413341e0a12e79cacb772347e48826564489219))

## [0.1.2](https://github.com/adr0ps1/Sabeltann/compare/v0.1.1...v0.1.2) (2026-06-13)


### Bug Fixes

* Correct action commit SHAs in release workflow ([0d1d65b](https://github.com/adr0ps1/Sabeltann/commit/0d1d65b07d46cfdddf731f349f3e0bc773c9f297))
* Replace invalid wixsharp CLI with WixSharp.bin NuGet package ([79d56c1](https://github.com/adr0ps1/Sabeltann/commit/79d56c185bb641f893d5988b74fb3c842995a62c))


### Chores

* Add release-please manifest ([2e3e6f4](https://github.com/adr0ps1/Sabeltann/commit/2e3e6f48b40bf26fabba4aa8ff4f44a624efe73e))
* **main:** Release 0.1.1 ([d6b155c](https://github.com/adr0ps1/Sabeltann/commit/d6b155c02c723e9883a41c2747aaaf2183db4ccd))
* **main:** Release 0.1.1 ([d6b155c](https://github.com/adr0ps1/Sabeltann/commit/d6b155c02c723e9883a41c2747aaaf2183db4ccd))
* **main:** Release 0.1.1 ([1f13c05](https://github.com/adr0ps1/Sabeltann/commit/1f13c055e9b1faadfaa577186c4d8f767de26a3a))
* Trigger fresh release cycle for v0.1.1 ([fee254d](https://github.com/adr0ps1/Sabeltann/commit/fee254d2ce19179774864708911d9213522ae728))
* Trigger release workflow with permissions fixed ([6750766](https://github.com/adr0ps1/Sabeltann/commit/675076608fc1d27243f668c8e7a4e9fc567c4fcf))


### Documentation

* Add emoji-packed README ([67bf69f](https://github.com/adr0ps1/Sabeltann/commit/67bf69fa8dcd6419b4f89692db4b3d07f9407edf))

## [0.1.1](https://github.com/adr0ps1/Sabeltann/compare/v0.1.0...v0.1.1) (2026-06-13)


### Bug Fixes

* Correct action commit SHAs in release workflow ([0d1d65b](https://github.com/adr0ps1/Sabeltann/commit/0d1d65b07d46cfdddf731f349f3e0bc773c9f297))


### Chores

* Add release-please manifest ([2e3e6f4](https://github.com/adr0ps1/Sabeltann/commit/2e3e6f48b40bf26fabba4aa8ff4f44a624efe73e))
* Trigger release workflow with permissions fixed ([6750766](https://github.com/adr0ps1/Sabeltann/commit/675076608fc1d27243f668c8e7a4e9fc567c4fcf))


### Documentation

* Add emoji-packed README ([67bf69f](https://github.com/adr0ps1/Sabeltann/commit/67bf69fa8dcd6419b4f89692db4b3d07f9407edf))
