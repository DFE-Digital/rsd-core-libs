# DfE.CoreLibs.Notifications.Contracts

A library providing contracts and abstractions for notification services, supporting multiple storage providers.

## Features

- **Provider-agnostic contracts** for notification services
- **Flexible notification models** with support for categorization, context, and metadata
- **Multiple storage abstractions** (session, Redis, database, in-memory)
- **User context abstraction** for multi-user scenarios
- **Comprehensive notification options** including auto-dismiss, priority levels, and action URLs

## Models

### Notification
Core notification model with properties for:
- Message content and type (Success, Error, Info, Warning)
- Auto-dismiss configuration
- Context and category for organization
- User identification and action URLs
- Priority levels and metadata

### NotificationOptions
Configuration class for customizing notification behavior:
- Context-based deduplication
- Category grouping
- Auto-dismiss settings
- Priority and metadata

## Interfaces

### INotificationService
Main service interface providing:
- Convenience methods for different notification types
- Full notification management (CRUD operations)
- Filtering by category, context, and read status
- Bulk operations

### INotificationStorage
Storage abstraction supporting:
- Multiple storage providers
- Async operations
- User-scoped data isolation

### IUserContextProvider
Context provider for:
- Session-based identification
- User-based identification
- Custom context resolution

## Usage

This package contains only contracts and models. Install the implementation package `DfE.CoreLibs.Notifications` for actual functionality.