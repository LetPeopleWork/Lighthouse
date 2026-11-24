// Write consent of one Azure DevOps OAuth connection (Story 6672).
// Rights stored on the credential: NotRecorded (signed in before the rights were stored),
// Read or ReadAndWrite (parsed from the token response of the last sign-in).
module writeConsent {
  type Rights = str      // "NotRecorded" | "Read" | "ReadAndWrite"
  type Requested = str   // "None" | "Read" | "ReadAndWrite"
  type WriteOutcome = str // "None" | "Written" | "Skipped"

  var mappings: bool
  var rights: Rights
  var lastRequested: Requested
  var lastWrite: WriteOutcome

  // WorkTrackingSystemConnection.NeededWorkItemRights
  pure def needed(hasMappings: bool): Rights = if (hasMappings) "ReadAndWrite" else "Read"

  // OAuthCredential.HasWriteConsent: only a recorded Read refuses
  pure def hasWriteConsent(r: Rights): bool = r != "Read"

  // DTO NeedsWriteConsent
  def needsWriteConsent: bool = mappings and not(hasWriteConsent(rights))

  action init = all {
    nondet m = Set(true, false).oneOf()
    nondet r = Set("NotRecorded", "Read", "ReadAndWrite").oneOf()
    all {
      mappings' = m,
      rights' = r,
      lastRequested' = "None",
      lastWrite' = "None",
    }
  }

  action addMapping = all {
    mappings' = true,
    rights' = rights,
    lastRequested' = "None",
    lastWrite' = "None",
  }

  action removeLastMapping = all {
    mappings' = false,
    rights' = rights,
    lastRequested' = "None",
    lastWrite' = "None",
  }

  // OAuthService.InitiateAsync/CompleteAsync: requests what the connection needs;
  // the identity platform may grant less than requested, never more.
  action signIn(grantsLess: bool): bool = {
    val requested = needed(mappings)
    all {
      mappings' = mappings,
      rights' = if (grantsLess) "Read" else requested,
      lastRequested' = requested,
      lastWrite' = "None",
    }
  }

  // OAuthService.PerformRefreshAsync leaves the stored rights alone.
  action refresh = all {
    mappings' = mappings,
    rights' = rights,
    lastRequested' = "None",
    lastWrite' = "None",
  }

  // WriteBackService.WriteFieldsToWorkItems: updates exist only when mappings exist.
  action writeBack = all {
    mappings,
    mappings' = mappings,
    rights' = rights,
    lastRequested' = "None",
    lastWrite' = if (hasWriteConsent(rights)) "Written" else "Skipped",
  }

  action step = any {
    addMapping,
    removeLastMapping,
    nondet g = Set(true, false).oneOf()
    signIn(g),
    refresh,
    writeBack,
  }

  // P2: no write without write consent.
  val noWriteWithoutConsent = lastWrite == "Written" implies hasWriteConsent(rights)
  // P1: a sign-in for a connection without mappings asks for read only.
  val signInLeastPrivilege = lastRequested == "ReadAndWrite" implies mappings
  // P3: a credential signed in before the rights were stored keeps writing.
  val notRecordedNeverSkipped = lastWrite == "Skipped" implies rights == "Read"
  // P4: every skipped write-back is shown to the admin as a needed sign-in.
  val skipShowsWarning = lastWrite == "Skipped" implies needsWriteConsent
  // P4: a sign-in that is granted what it asked for clears the warning.
  val grantedSignInClearsWarning =
    (lastRequested != "None" and rights == lastRequested) implies not(needsWriteConsent)

  val allInvariants = and {
    noWriteWithoutConsent,
    signInLeastPrivilege,
    notRecordedNeverSkipped,
    skipShowsWarning,
    grantedSignInClearsWarning,
  }
}
