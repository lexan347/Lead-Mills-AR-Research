# End-of-day routine

1. Stop recordings and close active build/deployment processes cleanly.
2. Save device, OS, Unity, package, provider, and model versions.
3. Copy small configuration and manifest files into the current iteration.
4. Calculate checksums for evidence retained outside Git.
5. Update the daily log with completed work, evidence, limitations, blockers, and exact restart step.
6. Update `DECISIONS.md` when a technology or methodology choice changes.
7. Run repository validation and inspect `git status` for secrets or large binaries.
8. Commit related changes with an evidence-based message.
9. Push only after reviewing the staged diff.
10. Record whether the day is paused or closed; do not manufacture entries for unattended time.
