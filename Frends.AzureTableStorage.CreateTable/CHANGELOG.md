# Changelog

## [1.0.0] - 2026-09-01

### Added

- Initial implementation of CreateTable task.
- Support for multiple authentication methods: ConnectionString, OAuth2, SasToken, ArcManagedIdentity and ArcManagedIdentityCrossTenant.
- Idempotent table creation with Created flag indicating whether table was newly created or already existed.
- FailIfTableExists option to control behavior when table already exists.
- Comprehensive error handling with detailed error messages and Azure error codes.
