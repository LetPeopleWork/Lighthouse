# Slice 29 — S11 `PublishDir` outside the project folder

Epic #6264 · `@infrastructure` · job_id: infrastructure-only · **Size S** · Pick-up group G0 (independent, can go first)

## Goal

Publish output stops nesting recursively inside the project (`Lighthouse.Backend/Lighthouse.Backend/publish/linux-x64/publish/...`, ~151 MB locally).

## IN

- Set `PublishDir` outside the project folder.
- Update every consumer of the old path in the same PR: CI workflows, release workflow, Dockerfiles, Standalone packaging scripts.

## OUT

- Any change to what is published.

## Hypothesis

Only path consumers need updating. **Disproved if** the published artefact changes (file set or size beyond noise).

## Acceptance criteria

- [ ] Given a clean checkout, when `dotnet publish` runs twice, then the output sits outside the project and does not nest.
- [ ] Published file set identical, and size within noise.
- [ ] CI and release pipelines green on the new path.

## Dependencies

None.

## Notes for DESIGN

Grep the workflows and the website/helm repos for the old path before changing it.

## Slicing

Carpaccio slicing happens at this Story's DESIGN. It is likely a single slice.
