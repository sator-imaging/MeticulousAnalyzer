# `Auto-dot-github.yml` Update Notification

> [!TIP]
> Update this file will notify the listener repos of changes.

```yml
name: '[Auto] Repo Gardening'

on:
  schedule:
    - cron: "0 23 * * *"
  workflow_dispatch:
  workflow_call:
  pull_request:
    types:
      - opened
      - edited
      - closed
      - reopened
  release:
    types:
      - published

jobs:
  reuse:
    uses: sator-imaging/.github/.github/workflows/Auto-dot-github.yml@main
    secrets: inherit
    permissions:
      pull-requests: write
      contents: write  # Sync .github | Auto changelog
      issues: write    # PR labeler
```
