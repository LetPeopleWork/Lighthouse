---
title: Standard Installation
layout: home
parent: Installation Overview
nav_order: 2
---

# Standard Installation

## Prerequisites
- Windows, MacOS, or Linux operating system
- Administrative access for initial setup
- 500MB free disk space

## Installation Steps

1. **Download Package**
   - Get latest version from [Releases](https://github.com/LetPeopleWork/Lighthouse/releases/latest)
   - Choose package matching your operating system
   - Extract to desired location

2. **Using Installation Scripts**

   Choose the appropriate script from [Scripts](https://github.com/LetPeopleWork/Lighthouse/tree/main/Scripts):
   
   Windows:
   ```powershell
   .\update_windows.ps1
   ```
   
   Linux:
   ```bash
   ./update_linux.sh
   ```
   
   MacOS:
   ```bash
   ./update_mac.sh
   ```

3. **Launch Application**
   - Run `Lighthouse` executable
   - Access web interface at [https://localhost:5001](https://localhost:5001)

## Updating

Use the same scripts mentioned above to update to the latest version. Your database and settings will be preserved.

See [Configuration](../configuration.md) for detailed configuration options.
