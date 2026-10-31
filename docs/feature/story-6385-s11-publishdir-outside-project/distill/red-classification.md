# RED classification (DISTILL, story 6385)

- NoBuildFileWritesOrReadsTheBackendPublishOutputInsideTheBackendProjectFolder: MISSING_FUNCTIONALITY. Un-ignored, it fails on its own assertion with 11 `file:line` offenders in the 4 consumer files; no setup, import or fixture error.
- TheScanSeesTheFilesThatConsumeThePublishOutput: not RED, green today by design (guards the guard against an empty scan).
- OnlyThePathInsideTheBackendProjectFolderIsRecognised (11 cases, including the lower-case Windows path and the JSON-escaped double backslash): not RED, green today by design (pattern specificity).
- Classifier widening (`.github/actions/…` must give `backend=true`, `github_changes` unchanged): a committed shell test to be written in DELIVER with the extraction of the `backend` rule into `path-classifier.sh`. It is red today in the sense that `.github/actions/…` gives `backend=false`; there is no test file for it yet, so there is nothing to classify.
- Verification-only items (new-path consumers, `.dockerignore` lines, behaviour-free publish comparison): no test.
