# Changelog

## [0.2.0](https://github.com/adr0ps1/Sabeltann/compare/v0.1.29...v0.2.0) (2026-08-09)


### ⚠ BREAKING CHANGES

* v1.0.0 release (breaking: stable API/UI line)

### Features

* Add VOD/series browsers, channel classifier, and WixSharp installer ([cf2d94e](https://github.com/adr0ps1/Sabeltann/commit/cf2d94e7bca2440d38f1ae918b672fb6cc5f3519))
* CC/subtitle selection via context menu with live track refresh ([9eaba79](https://github.com/adr0ps1/Sabeltann/commit/9eaba7928b8fd5381e5ddc32bfb11fde2aeb4962))
* Embed app icon in EXE; add ARPPRODUCTICON and shortcut icons to MSI ([332db5b](https://github.com/adr0ps1/Sabeltann/commit/332db5b8ee6ed392d5fb638ccebc42a6b0176c6f))
* Installation Status dialog under File menu ([#100](https://github.com/adr0ps1/Sabeltann/issues/100)) ([#101](https://github.com/adr0ps1/Sabeltann/issues/101)) ([35a8bce](https://github.com/adr0ps1/Sabeltann/commit/35a8bce5220ea2aabaa50d1dcc1afd5283be5710))
* MacOS installer (osx-x64 .pkg via Velopack) ([#120](https://github.com/adr0ps1/Sabeltann/issues/120)) ([9ba170d](https://github.com/adr0ps1/Sabeltann/commit/9ba170d2be8046686be03365d883ea35662ccd3c))
* Morphing-toolbar GUI redesign (slate + amber theme) ([#67](https://github.com/adr0ps1/Sabeltann/issues/67)) ([7889cdc](https://github.com/adr0ps1/Sabeltann/commit/7889cdc2732f95bb3fe55bdb23f482a64b165bbd))
* Persist and restore windowed size across sessions ([#91](https://github.com/adr0ps1/Sabeltann/issues/91)) ([b195463](https://github.com/adr0ps1/Sabeltann/commit/b195463d76155deecaf19af221cb56fa5300436f))
* Record live TV to file while it keeps playing ([#84](https://github.com/adr0ps1/Sabeltann/issues/84)) ([#119](https://github.com/adr0ps1/Sabeltann/issues/119)) ([df9ceb1](https://github.com/adr0ps1/Sabeltann/commit/df9ceb12c927a5eab3fc233e4a08bef7bfde3bea))
* Research/iptv features ([#52](https://github.com/adr0ps1/Sabeltann/issues/52)) ([b21f9dd](https://github.com/adr0ps1/Sabeltann/commit/b21f9dd8c81985d16392ff28cebec8e55f1b3da5))
* Settings window with default volume, auto-load toggle, connection info; cap initial channel display at 500 ([7ed2782](https://github.com/adr0ps1/Sabeltann/commit/7ed2782bc3f7f8bb74573cf126431f8012db45f0))
* Slate-palette popups + 'up to date' update toast ([#93](https://github.com/adr0ps1/Sabeltann/issues/93), [#94](https://github.com/adr0ps1/Sabeltann/issues/94)) ([#96](https://github.com/adr0ps1/Sabeltann/issues/96)) ([6176f23](https://github.com/adr0ps1/Sabeltann/commit/6176f236da91e773c5286d57bf1b61b473fa28cf))
* Smart channel split (Live vs VOD), transport auto-hide, X stop button ([2bfb263](https://github.com/adr0ps1/Sabeltann/commit/2bfb263952a186606015b5816772b265630a8a87))
* Space to pause/play keyboard shortcut ([1cb1da9](https://github.com/adr0ps1/Sabeltann/commit/1cb1da99494c59b91446d0d915b4418feb78da00))
* Switch to Velopack installer with auto-updates ([1431be9](https://github.com/adr0ps1/Sabeltann/commit/1431be93e2b2956ba506759d5fe2ea4572b008f9))
* Switch to Velopack installer with auto-updates ([aead998](https://github.com/adr0ps1/Sabeltann/commit/aead9986d055d1c1f34a841317b0d490288c4f60))
* Switch to Velopack installer with auto-updates ([#10](https://github.com/adr0ps1/Sabeltann/issues/10)) ([1431be9](https://github.com/adr0ps1/Sabeltann/commit/1431be93e2b2956ba506759d5fe2ea4572b008f9))
* V1.0.0 release (breaking: stable API/UI line) ([4eeef53](https://github.com/adr0ps1/Sabeltann/commit/4eeef534ff4ab9ae95651939805d8ac8eb96f4ec))
* VOD continue-watching, richer movie detail, audio tracks, pop-out player ([#56](https://github.com/adr0ps1/Sabeltann/issues/56)) ([c1167a4](https://github.com/adr0ps1/Sabeltann/commit/c1167a4efedafd60f022b1e822c8c083668b05db))


### Bug Fixes

* "All" category truncated series/live-tv lists ([#111](https://github.com/adr0ps1/Sabeltann/issues/111)) ([56a2ff2](https://github.com/adr0ps1/Sabeltann/commit/56a2ff2433020eabec9fa480209e6ad94eab8446))
* Accurate Live vs VOD split using provider group-title data; Movies & Series now shows content ([55862d6](https://github.com/adr0ps1/Sabeltann/commit/55862d64590383e06dcc015bf31809f7ea510b3c))
* Add missing UpdateDialog.axaml ([4f8d455](https://github.com/adr0ps1/Sabeltann/commit/4f8d4556ede0399578472dd2b84cfb32789e5732))
* Add missing UpdateDialog.axaml ([459b006](https://github.com/adr0ps1/Sabeltann/commit/459b006cc3b733499279ffe6e2c68a2809be5e97))
* ContentPicker spacing, autoplay on category select, group drill-down ([bb0d120](https://github.com/adr0ps1/Sabeltann/commit/bb0d120228fca3ead7576ed2037f3c0c5e21b4db))
* Correct action commit SHAs in release workflow ([0d1d65b](https://github.com/adr0ps1/Sabeltann/commit/0d1d65b07d46cfdddf731f349f3e0bc773c9f297))
* Debug overlay as popup over native video, UpdateDialog parameterless ctor ([5641ad8](https://github.com/adr0ps1/Sabeltann/commit/5641ad81655ebbf9ce2b340efe7d45d90d4630fc))
* Debug popup over video, UpdateDialog ctor fix ([c5c8f95](https://github.com/adr0ps1/Sabeltann/commit/c5c8f959756e99b295eb281a732c979e45e7de2d))
* Debug toggle button, crash logging, D-key fix, constructor cleanup ([73e2bfa](https://github.com/adr0ps1/Sabeltann/commit/73e2bfaf10fce55e3f1de985ccd799607b543e6b))
* Debug toggle button, crash logging, D-key no longer conflicts with search ([b8006e0](https://github.com/adr0ps1/Sabeltann/commit/b8006e00dc6c90bd501febf1a6b00e7bc451d9a5))
* ESC goes back to correct view, transport controls responsive, fix fullscreen-pause race ([#24](https://github.com/adr0ps1/Sabeltann/issues/24)) ([c7d709e](https://github.com/adr0ps1/Sabeltann/commit/c7d709ec541091842b6d073b6fce96e0fcbaa007))
* ESC in picker mode no longer goes to welcome when connected ([#28](https://github.com/adr0ps1/Sabeltann/issues/28)) ([0483d3b](https://github.com/adr0ps1/Sabeltann/commit/0483d3b94f00b9c04553b29c078424e6adc2bf48))
* Exact group-name matching for Live/VOD split; reconnection progress overlay ([987f819](https://github.com/adr0ps1/Sabeltann/commit/987f819e724e423e711f49f21065fc32def9bb27))
* Fixed image on all categories ([c2085f0](https://github.com/adr0ps1/Sabeltann/commit/c2085f00c5333461fd39bc5fd40fce6dfff337b2))
* Fixed the media controls except using space to pause, also fixed playback to be visible ([#36](https://github.com/adr0ps1/Sabeltann/issues/36)) ([269c751](https://github.com/adr0ps1/Sabeltann/commit/269c7514118a0df0427923e2c6156aaa9e13e29a))
* Fullscreen hides title bar and status bar completely ([a6d9c6a](https://github.com/adr0ps1/Sabeltann/commit/a6d9c6a2d7f54f8034c0de24ced78081d3b8b292))
* Fullscreen hides title bar completely ([2c01acd](https://github.com/adr0ps1/Sabeltann/commit/2c01acdd7ab2f2e21f2b32693cf80425aaf945f3))
* Fullscreen hides top title bar and bottom status bar via row collapsing ([16df6c5](https://github.com/adr0ps1/Sabeltann/commit/16df6c59346f71c36729a30cf03474f7101017a9))
* Fullscreen hides top title bar and bottom status bar via row collapsing ([47bb455](https://github.com/adr0ps1/Sabeltann/commit/47bb455876b13e56e07421c582a7fc1991734ec3))
* Keyboard shortcuts no longer fire when typing in search TextBox ([#30](https://github.com/adr0ps1/Sabeltann/issues/30)) ([58be983](https://github.com/adr0ps1/Sabeltann/commit/58be983e48f72d2f4a8b54332c58da0c3f2db8ee))
* Move AGENTS.md to global config, remove from repo ([b91e53a](https://github.com/adr0ps1/Sabeltann/commit/b91e53a642a6f76689f4ef5fa7509a1ee04b57f6))
* Movies & Shows grid with category cards, fix pending playlist re-use, fix back button ([3a57b17](https://github.com/adr0ps1/Sabeltann/commit/3a57b175a61ea5e91adc8b94f161b4182ca7b818))
* Popup overlays, buffering UI, VOD progress, movie detail page, polished auto-update, repo hardening ([#48](https://github.com/adr0ps1/Sabeltann/issues/48)) ([d8dae58](https://github.com/adr0ps1/Sabeltann/commit/d8dae5838aa9889e80c0999868f683c1af41b146))
* Popup transport bar with auto-hide, debug panel always on top, stop exits fullscreen ([b4c1667](https://github.com/adr0ps1/Sabeltann/commit/b4c1667bb76bf71437feb8eaaf443b24a2365e5c))
* Re-enable Sigstore attestation for public repo ([c482720](https://github.com/adr0ps1/Sabeltann/commit/c482720ac1f383ce7f6cf479f40c17fa710e1d4b))
* Regenerate icon and hide transport controls on minimize/deactivate ([8ab285c](https://github.com/adr0ps1/Sabeltann/commit/8ab285c5c97dcc7c3c4de1acc855c258286b431f))
* Regenerate icon and hide transport controls on minimize/deactivate ([bc52e97](https://github.com/adr0ps1/Sabeltann/commit/bc52e97c4f20abacdbfc32467bff9972bd73ac45))
* Remove CategoryGrid (causes freeze), both buttons show channel list ([69fd61a](https://github.com/adr0ps1/Sabeltann/commit/69fd61a966a580deb355f1786de5acced3969d70))
* Remove Sentry, add XamlLoader for .NET 10 compat, README license notice, search pool fix ([e61a854](https://github.com/adr0ps1/Sabeltann/commit/e61a8543930df8f95e0a51716021f31d8a0d27dc))
* Remove sigstore attestation (private repo); use WiX v4 CLI for MSI ([7791753](https://github.com/adr0ps1/Sabeltann/commit/77917530bf78fa1d4e1f7b39243472bfb3657e32))
* Replace invalid ICO with proper icon from SVG ([d50cd2c](https://github.com/adr0ps1/Sabeltann/commit/d50cd2c9f88e29d71a594fecfa0250d65100785b))
* Replace invalid wixsharp CLI with WixSharp.bin NuGet package ([79d56c1](https://github.com/adr0ps1/Sabeltann/commit/79d56c185bb641f893d5988b74fb3c842995a62c))
* Replace WixSharp with WiX v4 CLI for MSI installer ([8413341](https://github.com/adr0ps1/Sabeltann/commit/8413341e0a12e79cacb772347e48826564489219))
* Resolve issues [#33](https://github.com/adr0ps1/Sabeltann/issues/33), [#34](https://github.com/adr0ps1/Sabeltann/issues/34), [#31](https://github.com/adr0ps1/Sabeltann/issues/31), [#35](https://github.com/adr0ps1/Sabeltann/issues/35), [#26](https://github.com/adr0ps1/Sabeltann/issues/26), [#27](https://github.com/adr0ps1/Sabeltann/issues/27) + open source hardening ([#40](https://github.com/adr0ps1/Sabeltann/issues/40)) ([04a4d64](https://github.com/adr0ps1/Sabeltann/commit/04a4d64f36ed68c2bec86c271f8824943dab1a2c))
* Resolve merge conflicts, clean up constructor ([156bb5c](https://github.com/adr0ps1/Sabeltann/commit/156bb5c9db442369ee62a60a2c79957f46987f42))
* Restore InitializeComponent(), net10.0, drop XamlLoader, VLC 3.0.23.1 ([f171ea7](https://github.com/adr0ps1/Sabeltann/commit/f171ea7356e7f2373a0a1a8113c051d207f856f1))
* Sentry removal, XamlLoader for .NET 10 compat, search pool, README notice ([f592cbe](https://github.com/adr0ps1/Sabeltann/commit/f592cbe8fb8172e0c14c7a96dffd887d68080b84))
* Six player/popout/fullscreen/update bugs ([#80](https://github.com/adr0ps1/Sabeltann/issues/80)–[#86](https://github.com/adr0ps1/Sabeltann/issues/86)) ([#92](https://github.com/adr0ps1/Sabeltann/issues/92)) ([a39828c](https://github.com/adr0ps1/Sabeltann/commit/a39828cbba327a3038c204bd0817e0adc247f125))
* Target net472 for WixSharp installer so MSI builds ([a7584b1](https://github.com/adr0ps1/Sabeltann/commit/a7584b1f22898e6d4879c2fdc15bec6c86d7f43d))
* Transport controls over video, fullscreen pause bug, icon, update dialog ([5d624fa](https://github.com/adr0ps1/Sabeltann/commit/5d624fa977b4abf2effb110313867b61700d436c))
* Transport, fullscreen, icon, update dialog ([a3b2d0f](https://github.com/adr0ps1/Sabeltann/commit/a3b2d0fc8fe38a9714d5c23aee5663b42bfd2b43))
* Trigger workflows ([#38](https://github.com/adr0ps1/Sabeltann/issues/38)) ([984a5e7](https://github.com/adr0ps1/Sabeltann/commit/984a5e79c27e7aa247afc4d63ab948eb66e13507))
* Upgrade packages (consolidates dependabot [#41](https://github.com/adr0ps1/Sabeltann/issues/41)-46, [#54](https://github.com/adr0ps1/Sabeltann/issues/54), [#55](https://github.com/adr0ps1/Sabeltann/issues/55)) ([#77](https://github.com/adr0ps1/Sabeltann/issues/77)) ([a4d13fa](https://github.com/adr0ps1/Sabeltann/commit/a4d13fa62e4559ee727f89e130dd2d1b07e23619))
* Upgrade release-please to v5 (Node 24), remove Sentry references from docs ([4d7d309](https://github.com/adr0ps1/Sabeltann/commit/4d7d3091838a69014eb86dafbe05762ba66d6238))
* Use WixSharp 1.x and install WiX v3 toolchain for MSI build ([7aee6ae](https://github.com/adr0ps1/Sabeltann/commit/7aee6ae393b79ac16f29982aaa0b860fb5dbc66c))
* Vertically center skull in logo + app icon ([#90](https://github.com/adr0ps1/Sabeltann/issues/90)) ([#103](https://github.com/adr0ps1/Sabeltann/issues/103)) ([dd2b444](https://github.com/adr0ps1/Sabeltann/commit/dd2b44448a96f76b83f73a886672505cbb17ecd8))
* VLC null-safe, .NET 9 target, error handling ([1d4619a](https://github.com/adr0ps1/Sabeltann/commit/1d4619afdd8d87d54203493f6c8322a6ce0b092c))
* XamlLoader for .NET 10, search pool, Sentry cleanup, Settings window ([391393d](https://github.com/adr0ps1/Sabeltann/commit/391393d0f2fbf0200d71733107f32f300fadb7f9))
* XamlLoader populates fields even if Load fails; ConnectionPage uses FindControl ([cfd23c1](https://github.com/adr0ps1/Sabeltann/commit/cfd23c1906188798da8079758783b56fbd9e5a6e))


### Chores

* Add GPL-3.0 license, code signing policy, privacy policy ([eb20a0c](https://github.com/adr0ps1/Sabeltann/commit/eb20a0c0f507e9aabdfd90bd44bb2107639f956b))
* Add release-please manifest ([2e3e6f4](https://github.com/adr0ps1/Sabeltann/commit/2e3e6f48b40bf26fabba4aa8ff4f44a624efe73e))
* **ci:** Remove winget-pkgs publish job ([#87](https://github.com/adr0ps1/Sabeltann/issues/87)) ([b910903](https://github.com/adr0ps1/Sabeltann/commit/b9109034400c8d85115f0932b1518246670071b7))
* **deps:** Bump actions/checkout from 7.0.0 to 7.0.1 ([#113](https://github.com/adr0ps1/Sabeltann/issues/113)) ([509d64b](https://github.com/adr0ps1/Sabeltann/commit/509d64b1ed5fec20ee21d84bb149de1cd298af78))
* **deps:** Bump actions/setup-dotnet from 5.4.0 to 6.0.0 ([#112](https://github.com/adr0ps1/Sabeltann/issues/112)) ([4d03e9b](https://github.com/adr0ps1/Sabeltann/commit/4d03e9bdfe8c81f227a19872e171145f73e5e656))
* **deps:** Bump actions/upload-artifact from 4.6.0 to 7.0.1 ([#106](https://github.com/adr0ps1/Sabeltann/issues/106)) ([f192053](https://github.com/adr0ps1/Sabeltann/commit/f1920539413fd05cbb6bb3c3bdaf2c6a043c3b30))
* **deps:** Bump Avalonia.Desktop and Fonts.Inter to 12.1.1 ([e271399](https://github.com/adr0ps1/Sabeltann/commit/e2713995d232d5a971a5012d171a5b0de3c3201b))
* **deps:** Bump setup-dotnet 5.3.0-&gt;5.4.0, attest-build-provenance 4.1.0-&gt;4.1.1 ([#79](https://github.com/adr0ps1/Sabeltann/issues/79)) ([e0b7b5a](https://github.com/adr0ps1/Sabeltann/commit/e0b7b5a1af2d82edb346a5bd10080a44ef5c1a4c))
* Drop unused Svg.Skia dependency and stale handoff doc ([#89](https://github.com/adr0ps1/Sabeltann/issues/89)) ([67e8261](https://github.com/adr0ps1/Sabeltann/commit/67e82614ada83f8799d56e1816c0898eaaaeca45))
* **main:** Release 0.1.1 ([d6b155c](https://github.com/adr0ps1/Sabeltann/commit/d6b155c02c723e9883a41c2747aaaf2183db4ccd))
* **main:** Release 0.1.1 ([d6b155c](https://github.com/adr0ps1/Sabeltann/commit/d6b155c02c723e9883a41c2747aaaf2183db4ccd))
* **main:** Release 0.1.1 ([1f13c05](https://github.com/adr0ps1/Sabeltann/commit/1f13c055e9b1faadfaa577186c4d8f767de26a3a))
* **main:** Release 0.1.10 ([c893f73](https://github.com/adr0ps1/Sabeltann/commit/c893f73e8ca09eb4a20890f948814382e5dd6e02))
* **main:** Release 0.1.10 ([0b95e7d](https://github.com/adr0ps1/Sabeltann/commit/0b95e7d803d95b315d8d0dad7940dbe6150a4324))
* **main:** Release 0.1.10 ([#11](https://github.com/adr0ps1/Sabeltann/issues/11)) ([c893f73](https://github.com/adr0ps1/Sabeltann/commit/c893f73e8ca09eb4a20890f948814382e5dd6e02))
* **main:** Release 0.1.11 ([bea5f8a](https://github.com/adr0ps1/Sabeltann/commit/bea5f8ad76173ddd992bbbe4bbff75aaf2b64872))
* **main:** Release 0.1.11 ([17bec96](https://github.com/adr0ps1/Sabeltann/commit/17bec9636511260be864eb50cc34edf7f8065c0d))
* **main:** Release 0.1.12 ([178e39b](https://github.com/adr0ps1/Sabeltann/commit/178e39bfd251937cc8ad1ab4c9b58712044000e2))
* **main:** Release 0.1.12 ([bc06a78](https://github.com/adr0ps1/Sabeltann/commit/bc06a786bce275aa2d291320043cf62929b230cd))
* **main:** Release 0.1.13 ([d256ae6](https://github.com/adr0ps1/Sabeltann/commit/d256ae69f62fa487f5d75bfb7a664fa455a9d76e))
* **main:** Release 0.1.13 ([4169351](https://github.com/adr0ps1/Sabeltann/commit/416935145065be45a6c80f41965505f4ef4b3866))
* **main:** Release 0.1.14 ([2ae8567](https://github.com/adr0ps1/Sabeltann/commit/2ae8567f61a56da5fc836e7278eeb71f29e3a953))
* **main:** Release 0.1.14 ([48ed752](https://github.com/adr0ps1/Sabeltann/commit/48ed7523f0eeec6cb61051ab8934983aa8208ebe))
* **main:** Release 0.1.15 ([de4625c](https://github.com/adr0ps1/Sabeltann/commit/de4625c3ccf9872d690c047ecb43fcbaf5902527))
* **main:** Release 0.1.15 ([81d1c45](https://github.com/adr0ps1/Sabeltann/commit/81d1c4540a05c32b309a846357833195a2bc7962))
* **main:** Release 0.1.16 ([3d53336](https://github.com/adr0ps1/Sabeltann/commit/3d533362b1b67c45829d8f5d077ea105045b75fb))
* **main:** Release 0.1.16 ([7c5146c](https://github.com/adr0ps1/Sabeltann/commit/7c5146c164153f7c0a5b2eb8f569c5ac77cd5dcd))
* **main:** Release 0.1.17 ([#25](https://github.com/adr0ps1/Sabeltann/issues/25)) ([9795aa1](https://github.com/adr0ps1/Sabeltann/commit/9795aa1f1f978706ad47b3482da46b72431cf705))
* **main:** Release 0.1.18 ([#29](https://github.com/adr0ps1/Sabeltann/issues/29)) ([bac1e57](https://github.com/adr0ps1/Sabeltann/commit/bac1e578a1b7e3ac899f0b86e5e11cdc29f9bddd))
* **main:** Release 0.1.19 ([#39](https://github.com/adr0ps1/Sabeltann/issues/39)) ([9723465](https://github.com/adr0ps1/Sabeltann/commit/9723465e163a5f4e843224de6ab540d5e18cf5f6))
* **main:** Release 0.1.2 ([b47a443](https://github.com/adr0ps1/Sabeltann/commit/b47a443580cd5489442a8ec01572857341bf6f38))
* **main:** Release 0.1.2 ([b47a443](https://github.com/adr0ps1/Sabeltann/commit/b47a443580cd5489442a8ec01572857341bf6f38))
* **main:** Release 0.1.2 ([851b20c](https://github.com/adr0ps1/Sabeltann/commit/851b20cb46d72fa01788eab00a98df9d3b91a0e2))
* **main:** Release 0.1.20 ([#47](https://github.com/adr0ps1/Sabeltann/issues/47)) ([313913c](https://github.com/adr0ps1/Sabeltann/commit/313913c651445061700a281fc3f87848995ac1df))
* **main:** Release 0.1.21 ([#49](https://github.com/adr0ps1/Sabeltann/issues/49)) ([60e3378](https://github.com/adr0ps1/Sabeltann/commit/60e33788e72515ee8066a73138d6e7b6a5039259))
* **main:** Release 0.1.22 ([#51](https://github.com/adr0ps1/Sabeltann/issues/51)) ([57860b3](https://github.com/adr0ps1/Sabeltann/commit/57860b362383d9bd66655acf89feeef32cda2c0e))
* **main:** Release 0.1.23 ([#57](https://github.com/adr0ps1/Sabeltann/issues/57)) ([26b6c25](https://github.com/adr0ps1/Sabeltann/commit/26b6c25b6fd5b576e3ab3aa2fcaf822445a8b4ec))
* **main:** Release 0.1.24 ([#66](https://github.com/adr0ps1/Sabeltann/issues/66)) ([f09c897](https://github.com/adr0ps1/Sabeltann/commit/f09c897d7ddee3ac3bd430af1d4d5ebd024b6b41))
* **main:** Release 0.1.25 ([#78](https://github.com/adr0ps1/Sabeltann/issues/78)) ([d0be432](https://github.com/adr0ps1/Sabeltann/commit/d0be43211461de81da00b867cfd3410a53cada12))
* **main:** Release 0.1.26 ([#88](https://github.com/adr0ps1/Sabeltann/issues/88)) ([113bf9d](https://github.com/adr0ps1/Sabeltann/commit/113bf9d514dacc04b88ecadf7bfceb4b12a23952))
* **main:** Release 0.1.27 ([#97](https://github.com/adr0ps1/Sabeltann/issues/97)) ([b6c2b61](https://github.com/adr0ps1/Sabeltann/commit/b6c2b61688a14b544b99810ad16a5b4e4059b6ef))
* **main:** Release 0.1.28 ([#114](https://github.com/adr0ps1/Sabeltann/issues/114)) ([df23d2c](https://github.com/adr0ps1/Sabeltann/commit/df23d2c842686f83922ad6d5b3d2f3899b29e58b))
* **main:** Release 0.1.29 ([#121](https://github.com/adr0ps1/Sabeltann/issues/121)) ([7583321](https://github.com/adr0ps1/Sabeltann/commit/7583321caf634e49cdaba3f6844ce3c951bfd8f8))
* **main:** Release 0.1.3 ([9bcb09c](https://github.com/adr0ps1/Sabeltann/commit/9bcb09c56b86f56e2e010f079e6d903f9244f324))
* **main:** Release 0.1.3 ([9bcb09c](https://github.com/adr0ps1/Sabeltann/commit/9bcb09c56b86f56e2e010f079e6d903f9244f324))
* **main:** Release 0.1.3 ([1c82dcd](https://github.com/adr0ps1/Sabeltann/commit/1c82dcdbf255583389a35a931d9936946b7978d5))
* **main:** Release 0.1.4 ([87eefea](https://github.com/adr0ps1/Sabeltann/commit/87eefea66d35dcbfb9f30fc47f45496e74535c53))
* **main:** Release 0.1.4 ([87eefea](https://github.com/adr0ps1/Sabeltann/commit/87eefea66d35dcbfb9f30fc47f45496e74535c53))
* **main:** Release 0.1.4 ([54cc10f](https://github.com/adr0ps1/Sabeltann/commit/54cc10f8041df32d1bee28eeb370c8843d4697bd))
* **main:** Release 0.1.5 ([73aafe2](https://github.com/adr0ps1/Sabeltann/commit/73aafe2491cb41bb48b85db972aab82705920c61))
* **main:** Release 0.1.5 ([6c71e0d](https://github.com/adr0ps1/Sabeltann/commit/6c71e0ddfcc192fcd8852f08391ef66c358c4781))
* **main:** Release 0.1.6 ([ed31ede](https://github.com/adr0ps1/Sabeltann/commit/ed31ede124ed07b3dee4105c6f6c224c26a99704))
* **main:** Release 0.1.6 ([ed31ede](https://github.com/adr0ps1/Sabeltann/commit/ed31ede124ed07b3dee4105c6f6c224c26a99704))
* **main:** Release 0.1.6 ([976f4b0](https://github.com/adr0ps1/Sabeltann/commit/976f4b0f8513182c5b58adacf45e3bbfd9732d03))
* **main:** Release 0.1.7 ([#7](https://github.com/adr0ps1/Sabeltann/issues/7)) ([8a4e08f](https://github.com/adr0ps1/Sabeltann/commit/8a4e08f6bd33daac2104f9a77bad8d7d0fb20e03))
* **main:** Release 0.1.8 ([#8](https://github.com/adr0ps1/Sabeltann/issues/8)) ([1cb4787](https://github.com/adr0ps1/Sabeltann/commit/1cb4787ec4490c63f47bd9b96a68b22aaf4c6976))
* **main:** Release 0.1.9 ([5f345e0](https://github.com/adr0ps1/Sabeltann/commit/5f345e0c0dd05f4e63f9935c606d69d10431a232))
* **main:** Release 0.1.9 ([c80c1e8](https://github.com/adr0ps1/Sabeltann/commit/c80c1e8f74aa6ba7989780ec226ce0e106a4cdcb))
* **release:** Set next version to 1.0.0 ([ada66bb](https://github.com/adr0ps1/Sabeltann/commit/ada66bb1bb53e738fe09fe6d05175c81494e2983))
* Trigger fresh release cycle for v0.1.1 ([fee254d](https://github.com/adr0ps1/Sabeltann/commit/fee254d2ce19179774864708911d9213522ae728))
* Trigger release workflow with permissions fixed ([6750766](https://github.com/adr0ps1/Sabeltann/commit/675076608fc1d27243f668c8e7a4e9fc567c4fcf))
* Update readme.md ([defd68a](https://github.com/adr0ps1/Sabeltann/commit/defd68ade2fd2e8ea26ab028129b074c8b92e04f))
* Update readme.md ([9ce7f65](https://github.com/adr0ps1/Sabeltann/commit/9ce7f65210a49f68c4446d33ea6a9630b14ed283))


### Documentation

* Add emoji-packed README ([67bf69f](https://github.com/adr0ps1/Sabeltann/commit/67bf69fa8dcd6419b4f89692db4b3d07f9407edf))
* Add IPTV player feature research ([#50](https://github.com/adr0ps1/Sabeltann/issues/50)) ([d8dd7e3](https://github.com/adr0ps1/Sabeltann/commit/d8dd7e33312edb242d979d9c50c7f97223cb54ec))


### Refactors

* Ponytail pass — shared channel/grammar/image helpers ([#65](https://github.com/adr0ps1/Sabeltann/issues/65)) ([48a472e](https://github.com/adr0ps1/Sabeltann/commit/48a472ed3fba4f267165b31a64de7fea1ea1666f))

## [0.1.29](https://github.com/adr0ps1/Sabeltann/compare/v0.1.28...v0.1.29) (2026-08-09)


### Chores

* **deps:** Bump actions/checkout from 7.0.0 to 7.0.1 ([#113](https://github.com/adr0ps1/Sabeltann/issues/113)) ([509d64b](https://github.com/adr0ps1/Sabeltann/commit/509d64b1ed5fec20ee21d84bb149de1cd298af78))

## [0.1.28](https://github.com/adr0ps1/Sabeltann/compare/v0.1.27...v0.1.28) (2026-08-06)


### Features

* Record live TV to file while it keeps playing ([#84](https://github.com/adr0ps1/Sabeltann/issues/84)) ([#119](https://github.com/adr0ps1/Sabeltann/issues/119)) ([df9ceb1](https://github.com/adr0ps1/Sabeltann/commit/df9ceb12c927a5eab3fc233e4a08bef7bfde3bea))


### Chores

* **deps:** Bump actions/setup-dotnet from 5.4.0 to 6.0.0 ([#112](https://github.com/adr0ps1/Sabeltann/issues/112)) ([4d03e9b](https://github.com/adr0ps1/Sabeltann/commit/4d03e9bdfe8c81f227a19872e171145f73e5e656))
* **deps:** Bump actions/upload-artifact from 4.6.0 to 7.0.1 ([#106](https://github.com/adr0ps1/Sabeltann/issues/106)) ([f192053](https://github.com/adr0ps1/Sabeltann/commit/f1920539413fd05cbb6bb3c3bdaf2c6a043c3b30))

## [0.1.27](https://github.com/adr0ps1/Sabeltann/compare/v0.1.26...v0.1.27) (2026-07-19)


### Features

* Installation Status dialog under File menu ([#100](https://github.com/adr0ps1/Sabeltann/issues/100)) ([#101](https://github.com/adr0ps1/Sabeltann/issues/101)) ([35a8bce](https://github.com/adr0ps1/Sabeltann/commit/35a8bce5220ea2aabaa50d1dcc1afd5283be5710))
* Slate-palette popups + 'up to date' update toast ([#93](https://github.com/adr0ps1/Sabeltann/issues/93), [#94](https://github.com/adr0ps1/Sabeltann/issues/94)) ([#96](https://github.com/adr0ps1/Sabeltann/issues/96)) ([6176f23](https://github.com/adr0ps1/Sabeltann/commit/6176f236da91e773c5286d57bf1b61b473fa28cf))


### Bug Fixes

* "All" category truncated series/live-tv lists ([#111](https://github.com/adr0ps1/Sabeltann/issues/111)) ([56a2ff2](https://github.com/adr0ps1/Sabeltann/commit/56a2ff2433020eabec9fa480209e6ad94eab8446))
* Vertically center skull in logo + app icon ([#90](https://github.com/adr0ps1/Sabeltann/issues/90)) ([#103](https://github.com/adr0ps1/Sabeltann/issues/103)) ([dd2b444](https://github.com/adr0ps1/Sabeltann/commit/dd2b44448a96f76b83f73a886672505cbb17ecd8))

## [0.1.26](https://github.com/adr0ps1/Sabeltann/compare/v0.1.25...v0.1.26) (2026-07-08)


### Features

* Persist and restore windowed size across sessions ([#91](https://github.com/adr0ps1/Sabeltann/issues/91)) ([b195463](https://github.com/adr0ps1/Sabeltann/commit/b195463d76155deecaf19af221cb56fa5300436f))


### Bug Fixes

* Six player/popout/fullscreen/update bugs ([#80](https://github.com/adr0ps1/Sabeltann/issues/80)–[#86](https://github.com/adr0ps1/Sabeltann/issues/86)) ([#92](https://github.com/adr0ps1/Sabeltann/issues/92)) ([a39828c](https://github.com/adr0ps1/Sabeltann/commit/a39828cbba327a3038c204bd0817e0adc247f125))


### Chores

* **ci:** Remove winget-pkgs publish job ([#87](https://github.com/adr0ps1/Sabeltann/issues/87)) ([b910903](https://github.com/adr0ps1/Sabeltann/commit/b9109034400c8d85115f0932b1518246670071b7))
* Drop unused Svg.Skia dependency and stale handoff doc ([#89](https://github.com/adr0ps1/Sabeltann/issues/89)) ([67e8261](https://github.com/adr0ps1/Sabeltann/commit/67e82614ada83f8799d56e1816c0898eaaaeca45))

## [0.1.25](https://github.com/adr0ps1/Sabeltann/compare/v0.1.24...v0.1.25) (2026-07-07)


### Bug Fixes

* Upgrade packages (consolidates dependabot [#41](https://github.com/adr0ps1/Sabeltann/issues/41)-46, [#54](https://github.com/adr0ps1/Sabeltann/issues/54), [#55](https://github.com/adr0ps1/Sabeltann/issues/55)) ([#77](https://github.com/adr0ps1/Sabeltann/issues/77)) ([a4d13fa](https://github.com/adr0ps1/Sabeltann/commit/a4d13fa62e4559ee727f89e130dd2d1b07e23619))


### Chores

* **deps:** Bump setup-dotnet 5.3.0-&gt;5.4.0, attest-build-provenance 4.1.0-&gt;4.1.1 ([#79](https://github.com/adr0ps1/Sabeltann/issues/79)) ([e0b7b5a](https://github.com/adr0ps1/Sabeltann/commit/e0b7b5a1af2d82edb346a5bd10080a44ef5c1a4c))

## [0.1.24](https://github.com/adr0ps1/Sabeltann/compare/v0.1.23...v0.1.24) (2026-07-02)


### Features

* Morphing-toolbar GUI redesign (slate + amber theme) ([#67](https://github.com/adr0ps1/Sabeltann/issues/67)) ([7889cdc](https://github.com/adr0ps1/Sabeltann/commit/7889cdc2732f95bb3fe55bdb23f482a64b165bbd))


### Refactors

* Ponytail pass — shared channel/grammar/image helpers ([#65](https://github.com/adr0ps1/Sabeltann/issues/65)) ([48a472e](https://github.com/adr0ps1/Sabeltann/commit/48a472ed3fba4f267165b31a64de7fea1ea1666f))

## [0.1.23](https://github.com/adr0ps1/Sabeltann/compare/v0.1.22...v0.1.23) (2026-06-29)


### Features

* VOD continue-watching, richer movie detail, audio tracks, pop-out player ([#56](https://github.com/adr0ps1/Sabeltann/issues/56)) ([c1167a4](https://github.com/adr0ps1/Sabeltann/commit/c1167a4efedafd60f022b1e822c8c083668b05db))

## [0.1.22](https://github.com/adr0ps1/Sabeltann/compare/v0.1.21...v0.1.22) (2026-06-28)


### Features

* Research/iptv features ([#52](https://github.com/adr0ps1/Sabeltann/issues/52)) ([b21f9dd](https://github.com/adr0ps1/Sabeltann/commit/b21f9dd8c81985d16392ff28cebec8e55f1b3da5))


### Documentation

* Add IPTV player feature research ([#50](https://github.com/adr0ps1/Sabeltann/issues/50)) ([d8dd7e3](https://github.com/adr0ps1/Sabeltann/commit/d8dd7e33312edb242d979d9c50c7f97223cb54ec))

## [0.1.21](https://github.com/adr0ps1/Sabeltann/compare/v0.1.20...v0.1.21) (2026-06-23)


### Bug Fixes

* Popup overlays, buffering UI, VOD progress, movie detail page, polished auto-update, repo hardening ([#48](https://github.com/adr0ps1/Sabeltann/issues/48)) ([d8dae58](https://github.com/adr0ps1/Sabeltann/commit/d8dae5838aa9889e80c0999868f683c1af41b146))

## [0.1.20](https://github.com/adr0ps1/Sabeltann/compare/v0.1.19...v0.1.20) (2026-06-22)


### Bug Fixes

* Resolve issues [#33](https://github.com/adr0ps1/Sabeltann/issues/33), [#34](https://github.com/adr0ps1/Sabeltann/issues/34), [#31](https://github.com/adr0ps1/Sabeltann/issues/31), [#35](https://github.com/adr0ps1/Sabeltann/issues/35), [#26](https://github.com/adr0ps1/Sabeltann/issues/26), [#27](https://github.com/adr0ps1/Sabeltann/issues/27) + open source hardening ([#40](https://github.com/adr0ps1/Sabeltann/issues/40)) ([04a4d64](https://github.com/adr0ps1/Sabeltann/commit/04a4d64f36ed68c2bec86c271f8824943dab1a2c))

## [0.1.19](https://github.com/adr0ps1/Sabeltann/compare/v0.1.18...v0.1.19) (2026-06-20)


### Bug Fixes

* Trigger workflows ([#38](https://github.com/adr0ps1/Sabeltann/issues/38)) ([984a5e7](https://github.com/adr0ps1/Sabeltann/commit/984a5e79c27e7aa247afc4d63ab948eb66e13507))

## [0.1.18](https://github.com/adr0ps1/Sabeltann/compare/v0.1.17...v0.1.18) (2026-06-20)


### Bug Fixes

* ESC in picker mode no longer goes to welcome when connected ([#28](https://github.com/adr0ps1/Sabeltann/issues/28)) ([0483d3b](https://github.com/adr0ps1/Sabeltann/commit/0483d3b94f00b9c04553b29c078424e6adc2bf48))
* Fixed the media controls except using space to pause, also fixed playback to be visible ([#36](https://github.com/adr0ps1/Sabeltann/issues/36)) ([269c751](https://github.com/adr0ps1/Sabeltann/commit/269c7514118a0df0427923e2c6156aaa9e13e29a))
* Keyboard shortcuts no longer fire when typing in search TextBox ([#30](https://github.com/adr0ps1/Sabeltann/issues/30)) ([58be983](https://github.com/adr0ps1/Sabeltann/commit/58be983e48f72d2f4a8b54332c58da0c3f2db8ee))

## [0.1.17](https://github.com/adr0ps1/Sabeltann/compare/v0.1.16...v0.1.17) (2026-06-19)


### Bug Fixes

* ESC goes back to correct view, transport controls responsive, fix fullscreen-pause race ([#24](https://github.com/adr0ps1/Sabeltann/issues/24)) ([c7d709e](https://github.com/adr0ps1/Sabeltann/commit/c7d709ec541091842b6d073b6fce96e0fcbaa007))

## [0.1.16](https://github.com/adr0ps1/Sabeltann/compare/v0.1.15...v0.1.16) (2026-06-19)


### Bug Fixes

* Fixed image on all categories ([c2085f0](https://github.com/adr0ps1/Sabeltann/commit/c2085f00c5333461fd39bc5fd40fce6dfff337b2))

## [0.1.15](https://github.com/adr0ps1/Sabeltann/compare/v0.1.14...v0.1.15) (2026-06-19)


### Bug Fixes

* Add missing UpdateDialog.axaml ([4f8d455](https://github.com/adr0ps1/Sabeltann/commit/4f8d4556ede0399578472dd2b84cfb32789e5732))
* Add missing UpdateDialog.axaml ([459b006](https://github.com/adr0ps1/Sabeltann/commit/459b006cc3b733499279ffe6e2c68a2809be5e97))

## [0.1.14](https://github.com/adr0ps1/Sabeltann/compare/v0.1.13...v0.1.14) (2026-06-19)


### Bug Fixes

* Debug overlay as popup over native video, UpdateDialog parameterless ctor ([5641ad8](https://github.com/adr0ps1/Sabeltann/commit/5641ad81655ebbf9ce2b340efe7d45d90d4630fc))
* Debug popup over video, UpdateDialog ctor fix ([c5c8f95](https://github.com/adr0ps1/Sabeltann/commit/c5c8f959756e99b295eb281a732c979e45e7de2d))

## [0.1.13](https://github.com/adr0ps1/Sabeltann/compare/v0.1.12...v0.1.13) (2026-06-19)


### Bug Fixes

* Transport controls over video, fullscreen pause bug, icon, update dialog ([5d624fa](https://github.com/adr0ps1/Sabeltann/commit/5d624fa977b4abf2effb110313867b61700d436c))
* Transport, fullscreen, icon, update dialog ([a3b2d0f](https://github.com/adr0ps1/Sabeltann/commit/a3b2d0fc8fe38a9714d5c23aee5663b42bfd2b43))


### Chores

* Update readme.md ([defd68a](https://github.com/adr0ps1/Sabeltann/commit/defd68ade2fd2e8ea26ab028129b074c8b92e04f))
* Update readme.md ([9ce7f65](https://github.com/adr0ps1/Sabeltann/commit/9ce7f65210a49f68c4446d33ea6a9630b14ed283))

## [0.1.12](https://github.com/adr0ps1/Sabeltann/compare/v0.1.11...v0.1.12) (2026-06-19)


### Bug Fixes

* Upgrade release-please to v5 (Node 24), remove Sentry references from docs ([4d7d309](https://github.com/adr0ps1/Sabeltann/commit/4d7d3091838a69014eb86dafbe05762ba66d6238))

## [0.1.11](https://github.com/adr0ps1/Sabeltann/compare/v0.1.10...v0.1.11) (2026-06-19)


### Bug Fixes

* Regenerate icon and hide transport controls on minimize/deactivate ([bc52e97](https://github.com/adr0ps1/Sabeltann/commit/bc52e97c4f20abacdbfc32467bff9972bd73ac45))

## [0.1.10](https://github.com/adr0ps1/Sabeltann/compare/v0.1.9...v0.1.10) (2026-06-19)


### Features

* Switch to Velopack installer with auto-updates ([1431be9](https://github.com/adr0ps1/Sabeltann/commit/1431be93e2b2956ba506759d5fe2ea4572b008f9))
* Switch to Velopack installer with auto-updates ([aead998](https://github.com/adr0ps1/Sabeltann/commit/aead9986d055d1c1f34a841317b0d490288c4f60))
* Switch to Velopack installer with auto-updates ([#10](https://github.com/adr0ps1/Sabeltann/issues/10)) ([1431be9](https://github.com/adr0ps1/Sabeltann/commit/1431be93e2b2956ba506759d5fe2ea4572b008f9))


### Bug Fixes

* Target net472 for WixSharp installer so MSI builds ([a7584b1](https://github.com/adr0ps1/Sabeltann/commit/a7584b1f22898e6d4879c2fdc15bec6c86d7f43d))

## [0.1.9](https://github.com/adr0ps1/Sabeltann/compare/v0.1.8...v0.1.9) (2026-06-18)


### Bug Fixes

* Use WixSharp 1.x and install WiX v3 toolchain for MSI build ([7aee6ae](https://github.com/adr0ps1/Sabeltann/commit/7aee6ae393b79ac16f29982aaa0b860fb5dbc66c))

## [0.1.8](https://github.com/adr0ps1/Sabeltann/compare/v0.1.7...v0.1.8) (2026-06-18)


### Features

* Add VOD/series browsers, channel classifier, and WixSharp installer ([cf2d94e](https://github.com/adr0ps1/Sabeltann/commit/cf2d94e7bca2440d38f1ae918b672fb6cc5f3519))
* CC/subtitle selection via context menu with live track refresh ([9eaba79](https://github.com/adr0ps1/Sabeltann/commit/9eaba7928b8fd5381e5ddc32bfb11fde2aeb4962))


### Bug Fixes

* Restore InitializeComponent(), net10.0, drop XamlLoader, VLC 3.0.23.1 ([f171ea7](https://github.com/adr0ps1/Sabeltann/commit/f171ea7356e7f2373a0a1a8113c051d207f856f1))

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
