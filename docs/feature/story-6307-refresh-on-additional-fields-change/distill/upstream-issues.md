# Upstream issues from DISTILL — story-6307-refresh-on-additional-fields-change

Quinn, DISTILL 2026-10-15. None of these blocks the scenarios: the maintainer's sketch decisions (S1 to S7) already
settle each conflict, and the scenarios follow them. What is left is for the earlier waves' documents to say the same
thing. The three items that needed the maintainer were decided in AFK by the maintainer's proxy (items 10 to 12);
revisit them when he is back.

## For DESIGN

1. **Resolved by DESIGN (DDD-17): the counts are on the list of connections, through a read-only connection-usage
   port.** As first raised: DDD-9 and S4 say "the connection read the
   page already makes returns the two counts". The edit page reads its connection from
   `GET /api/latest/worktrackingsystemconnections` (the System Admin list, `getConfiguredWorkTrackingSystems`) and
   picks it out by id; it never calls the read by id. The scenarios therefore pin `usedBy { teams, portfolios }` on
   every connection in the list (`Slice02ConnectionsSayWhoUsesThemTest`). DESIGN's driving-port list and DDD-9 should
   say so. The read by id is still the one that registers a tracker's own fields, which is why it is not the place
   for this either way.
2. **Resolved by DESIGN: the save-result DTO is gone** (DDD-6 superseded; brief and ADR-239 point 3 struck out). As
   first raised: `docs/product/architecture/brief.md`
   invariant 6307-I4, the component list's `WorkTrackingSystemConnectionSaveResultDto`, and ADR-239 decision point 3
   still say the PUT answers with `queuedUpdates { teams, portfolios }`. No scenario asks for it and the page does not
   read it. DESIGN amends brief and ADR (or DELIVER does, as part of slice 01's documentation step); until then a
   crafter reading the brief would build a DTO nobody uses. The DEVOPS Lighthouse-Clients row also mentions
   `queuedUpdates` and needs the same edit.
3. **The fan-out event and handler are not pinned by name.** The scenarios observe the Task Manager and the refresh
   history, not `ConnectionFetchShapeChanged`. DELIVER is free to shape the event as DESIGN says; a unit test of the
   handler (Teams before Portfolios, one trigger each) is DELIVER's inner loop.
4. **Two of DESIGN's "publishes nothing" paths cannot be driven through HTTP with a stand-in tracker**, and are left
   to DESIGN's structural rule (only the PUT publishes) plus DELIVER's unit tests:
   - Jira's sync-path registration of its own fields runs inside the Jira connector, which the acceptance host
     replaces. The read-by-id registration is covered (`Looking_up_a_connection_that_registers_the_trackers_own_field_starts_no_update`
     and the save after it).
   - A handler that throws after the save is stored: the dispatcher awaits handlers inline, but
     `DomainEventDispatcher.InvokeHandlerSafely` catches and logs every handler exception, so the PUT answers
     **200** with the field saved. (Corrected 2026-10-15 by DESIGN; this item first said 500.) The controller's own
     owner lookup and publish after `Save()` are caught and logged the same way (DDD-16). Nothing in the host can be
     made to throw there without replacing production code; DELIVER pins both with unit tests, as DESIGN assigns them (DDD-16). This is the completeness audit's C7b gap at
     the acceptance level.
5. **The page and the server decide "the fields changed" separately.** The page compares the field list it loaded
   with the one it is about to save; the server compares the connection's share of the fetch fingerprint. They agree
   on every edit the page can make (add, rename, new reference, remove; the page cannot reorder fields). If a future
   edit makes them disagree, the page would ask a question the server then ignores, or the reverse. Noted, not
   tested.

## For DISCUSS

6. **US-02 is reshaped into the question before the save.** S1, S3 and S4 replace the after-save notice (AC-2.1,
   AC-2.2, AC-2.4, AC-2.5) with the dialog's title and body, before anything is saved. AC-2.3 (Terminology, singular
   and plural) survives unchanged. AC-2.1's "counted from what was actually queued" becomes "counted from the
   Teams and Portfolios on the connection", because the question is asked before anything is queued. The two only
   differ when a Team or Portfolio is already updating, and then the dialog's count is still the number of updates
   the Task Manager will show.
7. **AC-1.8 is replaced by S2**: the "next refresh of data" alert is removed and nothing replaces it while editing.
   The question carries the message instead.
8. **AC-1.5 is read as UC-1's accepted wording.** The scenario
   `A_Team_already_updating_or_waiting_is_not_downloaded_in_full_twice` pins it: Atlas (running) and Cobalt
   (waiting) each run twice, each downloads everything once, every other owner once, and the Task Manager lists each
   one once.
9. **AC-3.4's "the dialog appears only when saving would queue at least one update"** is read through S5: the
   question shows when the fields changed and the connection has at least one Team or Portfolio.

## For DEVOPS

10. **DO-2 changes timing and shape (S7).** The event carries `updates: Now | Later` and is sent only on the
    question's answer. DEVOPS put it in slice 02; this DISTILL puts it in slice 03, because its one property only
    exists once there are two answers, and the KPI it serves (`OUT-6307-opt-out-rate`) only exists then. **Decided in
    AFK, revisit:** the event lives in slice 03.

## For the maintainer — decided in AFK, revisit

11. **A count of zero of one kind keeps its place in the title.** Decided in AFK, revisit: it follows the
    maintainer's own pattern, `Update 3 Teams and 0 Portfolios?` and `Update 0 Teams and 2 Portfolios?`, both pinned
    in the frontend title table.
12. **A save refused after the question tells usage data nothing.** Decided in AFK, revisit: the answer only
    counts once the save it chose has gone through.
