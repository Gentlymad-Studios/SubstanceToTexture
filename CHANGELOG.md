# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.0.1] - 2023-03-23
### Added
- Initial release

## [0.0.2] - 2023-12-14
### Added
- Exposed the API so it is easier to create custom logic for creating substances
- Added new base SubstanceType class that is less convoluted
- image type can now be specified for every substance

## [0.0.3] - 2023-12-16
### Added
- Complete overhaul to make substance to texture generation much more generic
- Added ability to create multi path substance exports
### Removed
- Removed file watcher as we have enough preprocessors already!
- Removed old project settings approach

