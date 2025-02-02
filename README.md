# TestApi - Long Task Manager Web API

## Overview

`TestApi` provides a simple web API for managing long-running background tasks. It allows you to create tasks, execute them in the background, and fetch their output once completed. The tasks are logged, and the logs can be retrieved through a GUID associated with each task.

This API is designed to handle background work that takes some time to process, such as processing large datasets, sending emails, or handling other long-running operations.

## Features

- **Task Creation**: Create a new long-running task, which is queued for execution.
- **Task Execution**: Tasks run asynchronously in the background.
- **Task Output Retrieval**: Retrieve logs and completion status of a task using its unique identifier (GUID).
- **In-Memory Logging**: Logs are collected during the task's execution and can be retrieved once the task is completed.

## Prerequisites

To run this API, you need to have the following tools installed:

- .NET 9.0 or higher
- Visual Studio or any other .NET-compatible IDE (optional)

