# Contributing

## Branch and review practice

1. Start from the current `main` branch.
2. Use a short descriptive branch name.
3. Keep generated Unity caches, builds, credentials, and raw field captures out of Git.
4. Run the validation commands in the README.
5. Update the current iteration log and decision record when behavior, scope, or evidence changes.
6. Use a pull request for changes to production anchors, privacy behavior, historical interpretation, or deployment configuration.

## Unity handoff

- Use the exact Unity editor version recorded in the project.
- Commit `.meta` files.
- Pull before opening the project on another computer.
- Close the project on one computer before making overlapping scene or project-setting edits on another.
- Prefer text-serialized Unity assets and visible metadata files.

## Evidence language

Use one of these labels when relevant:

- **Demonstrated:** reproduced with retained evidence.
- **User-reported:** reported but not independently inspected.
- **Vendor claim:** stated by a service provider; not independently validated.
- **Assumption:** needed for planning and awaiting evidence.
- **Planned:** not yet performed.
