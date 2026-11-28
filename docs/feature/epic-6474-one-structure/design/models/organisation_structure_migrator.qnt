// The startup migrator that gives a connection its item-type mapping per level and every unit its subset, and
// re-derives a subset after a rollback to the previous release. One connection, one Team (level 1), one
// Portfolio (level 2), two server instances. Spellings are integers; Fold maps a spelling to its case-folded
// identity (1 "Bug" and 2 "bug" are one type). A second connection, added after the upgrade, has one
// Team saved on it later. Majority spelling and order are left to the planner's property
// tests; the model keeps folded identities only.
module organisation_structure_migrator {
  pure val Units: Set[str] = Set("team", "portfolio")
  pure val LevelOf: str -> int = Map("team" -> 1, "portfolio" -> 2)
  pure val Spellings: Set[int] = Set(1, 2, 3, 4)
  pure def Fold(s: int): int = if (s == 2) 1 else s
  pure def FoldAll(xs: Set[int]): Set[int] = xs.map(x => Fold(x))
  // today's create defaults: User Story (4), Bug (1) on level 1; Epic (3) on level 2
  pure val Defaults: int -> Set[int] = Map(1 -> Set(4, 1), 2 -> Set(3))

  // OrganisationStructureMigrationPlan.From: each level gets the union of its own units' types (no level wins).
  pure def Plan(old: str -> Set[int]): int -> Set[int] =
    Map(1 -> (if (old.get("team").size() == 0) Defaults.get(1) else FoldAll(old.get("team"))),
        2 -> (if (old.get("portfolio").size() == 0) Defaults.get(2) else FoldAll(old.get("portfolio"))))

  var oldTypes: str -> Set[int]       // Team/Portfolio.WorkItemTypes, the column the previous release reads
  var asWritten: str -> Set[int]      // WorkItemTypesAsWritten, what this release last wrote into that column
  var structureRows: int              // OrganisationStructures rows under the fixed key
  var mapping: int -> Set[int]        // ConnectionLevelMappings.ItemTypes
  var unitSubset: str -> Set[int]         // ItemTypeSubset
  var needsTypes: str -> bool         // NeedsItemTypes
  var phase: int -> str               // per instance: idle | read | done
  var sawStructure: int -> bool
  // ghosts
  var planned: int -> Set[int]
  var upgradeOld: str -> Set[int]
  var upgradeSubset: str -> Set[int]
  var reAdded: Set[(int, int)]
  var editedByPrevious: Set[str]
  // the connection added after the upgrade
  var lateRow: bool                   // its ConnectionMappings row exists
  var lateMapping: Set[int]           // its level-1 list
  var lateSubset: Set[int]            // the subset of the Team saved on it
  var lateOutcome: str                // none | saved | refused
  var lateRequested: Set[int]

  action unchangedGhosts = all {
    planned' = planned, upgradeOld' = upgradeOld, upgradeSubset' = upgradeSubset,
    reAdded' = reAdded, editedByPrevious' = editedByPrevious, lateRow' = lateRow, lateMapping' = lateMapping, lateSubset' = lateSubset, lateOutcome' = lateOutcome, lateRequested' = lateRequested,
  }

  action init = {
    nondet t = Spellings.powerset().oneOf()
    nondet p = Spellings.powerset().oneOf()
    all {
      oldTypes' = Map("team" -> t, "portfolio" -> p),
      asWritten' = Map("team" -> t, "portfolio" -> p),
      structureRows' = 0,
      mapping' = Map(1 -> Set(), 2 -> Set()),
      unitSubset' = Map("team" -> Set(), "portfolio" -> Set()),
      needsTypes' = Map("team" -> false, "portfolio" -> false),
      phase' = Map(1 -> "idle", 2 -> "idle"),
      sawStructure' = Map(1 -> false, 2 -> false),
      planned' = Map(1 -> Set(), 2 -> Set()),
      upgradeOld' = Map("team" -> Set(), "portfolio" -> Set()),
      upgradeSubset' = Map("team" -> Set(), "portfolio" -> Set()),
      reAdded' = Set(),
      editedByPrevious' = Set(),
      lateRow' = false, lateMapping' = Set(), lateSubset' = Set(), lateOutcome' = "none", lateRequested' = Set(),
    }
  }

  // OrganisationStructureMigrator.SeedAsync, first half: read whether the structure row exists.
  action begin(i: int): bool = all {
    phase.get(i) == "idle",
    phase' = phase.set(i, "read"),
    sawStructure' = sawStructure.set(i, structureRows > 0),
    oldTypes' = oldTypes, asWritten' = asWritten, structureRows' = structureRows, mapping' = mapping,
    unitSubset' = unitSubset, needsTypes' = needsTypes, unchangedGhosts,
  }

  // Second half, one transaction; the fixed-key insert fails when another instance already committed.
  action commitFirstUpgrade(i: int): bool = {
    val m = Plan(oldTypes)
    val s = Units.mapBy(u => FoldAll(oldTypes.get(u)).intersect(m.get(LevelOf.get(u))))
    all {
      phase.get(i) == "read", not(sawStructure.get(i)),
      if (structureRows == 0) all {
        structureRows' = 1, mapping' = m, unitSubset' = s,
        needsTypes' = Units.mapBy(u => s.get(u).size() == 0),
        asWritten' = oldTypes,
        planned' = m, upgradeOld' = Units.mapBy(u => FoldAll(oldTypes.get(u))), upgradeSubset' = s,
      } else all {
        structureRows' = structureRows, mapping' = mapping, unitSubset' = unitSubset, needsTypes' = needsTypes,
        asWritten' = asWritten, planned' = planned, upgradeOld' = upgradeOld, upgradeSubset' = upgradeSubset,
      },
      phase' = phase.set(i, "done"),
      sawStructure' = sawStructure, oldTypes' = oldTypes, reAdded' = reAdded, editedByPrevious' = editedByPrevious, lateRow' = lateRow, lateMapping' = lateMapping, lateSubset' = lateSubset, lateOutcome' = lateOutcome, lateRequested' = lateRequested,
    }
  }

  // Later starts: re-derive only a subset whose WorkItemTypes column differs from what this release wrote there.
  action rederive(i: int): bool = {
    val changed = Units.filter(u => oldTypes.get(u) != asWritten.get(u))
    val s = Units.mapBy(u =>
      if (changed.contains(u)) FoldAll(oldTypes.get(u)).intersect(mapping.get(LevelOf.get(u))) else unitSubset.get(u))
    all {
      phase.get(i) == "read", sawStructure.get(i),
      unitSubset' = s,
      needsTypes' = Units.mapBy(u => if (changed.contains(u)) s.get(u).size() == 0 else needsTypes.get(u)),
      asWritten' = oldTypes,
      phase' = phase.set(i, "done"),
      sawStructure' = sawStructure, oldTypes' = oldTypes, structureRows' = structureRows, mapping' = mapping,
      unchangedGhosts,
    }
  }

  // A process dies before its commit: its transaction never happened, and it starts again.
  action crash(i: int): bool = all {
    phase.get(i) == "read",
    phase' = phase.set(i, "idle"), sawStructure' = sawStructure.set(i, false),
    oldTypes' = oldTypes, asWritten' = asWritten, structureRows' = structureRows, mapping' = mapping,
    unitSubset' = unitSubset, needsTypes' = needsTypes, unchangedGhosts,
  }

  // Rollback: the previous release runs, edits one unit's WorkItemTypes, and this release starts again.
  action previousReleaseEdits(u: str, t: Set[int]): bool = all {
    structureRows > 0, phase.get(1) != "read", phase.get(2) != "read",
    oldTypes' = oldTypes.set(u, t),
    phase' = Map(1 -> "idle", 2 -> "idle"), sawStructure' = Map(1 -> false, 2 -> false),
    editedByPrevious' = editedByPrevious.union(Set(u)),
    asWritten' = asWritten, structureRows' = structureRows, mapping' = mapping, unitSubset' = unitSubset,
    needsTypes' = needsTypes, planned' = planned, upgradeOld' = upgradeOld, upgradeSubset' = upgradeSubset,
    reAdded' = reAdded, lateRow' = lateRow, lateMapping' = lateMapping, lateSubset' = lateSubset, lateOutcome' = lateOutcome, lateRequested' = lateRequested,
  }

  // A System Admin removes a mapped type (units that tick it are narrowed in the same transaction; the save
  // dual-writes WorkItemTypes, and a unit left empty keeps its last list there) and later re-adds it, ticked
  // for no unit.
  action removeThenReAdd(lvl: int, t: int): bool = {
    val narrowed = Units.mapBy(u => if (LevelOf.get(u) == lvl) unitSubset.get(u).exclude(Set(t)) else unitSubset.get(u))
    val written = Units.mapBy(u => if (narrowed.get(u).size() == 0) oldTypes.get(u) else narrowed.get(u))
    all {
      structureRows > 0, mapping.get(lvl).contains(t), phase.get(1) != "read", phase.get(2) != "read",
      unitSubset' = narrowed,
      needsTypes' = Units.mapBy(u => narrowed.get(u).size() == 0),
      oldTypes' = written, asWritten' = written,
      reAdded' = reAdded.union(Set((lvl, t))),
      mapping' = mapping, structureRows' = structureRows, phase' = phase, sawStructure' = sawStructure,
      planned' = planned, upgradeOld' = upgradeOld, upgradeSubset' = upgradeSubset,
      editedByPrevious' = editedByPrevious, lateRow' = lateRow, lateMapping' = lateMapping, lateSubset' = lateSubset, lateOutcome' = lateOutcome, lateRequested' = lateRequested,
    }
  }

  // ConnectionMapping.ForConnection: a connection with no mapping row gets one with today's create defaults the first
  // time it is needed (first Resolve or first unit create on it), in the same transaction, never from the unit's types.
  action saveTeamOnNewConnection(t: Set[int]): bool = {
    val m = if (not(lateRow)) Defaults.get(1) else lateMapping
    val ok = FoldAll(t).subseteq(m)
    all {
      structureRows > 0,
      lateRow' = true, lateMapping' = m,
      lateSubset' = if (ok) FoldAll(t) else lateSubset,
      lateOutcome' = if (ok) "saved" else "refused",
      lateRequested' = t,
      oldTypes' = oldTypes, asWritten' = asWritten, structureRows' = structureRows, mapping' = mapping,
      unitSubset' = unitSubset, needsTypes' = needsTypes, phase' = phase, sawStructure' = sawStructure,
      planned' = planned, upgradeOld' = upgradeOld, upgradeSubset' = upgradeSubset, reAdded' = reAdded,
      editedByPrevious' = editedByPrevious,
    }
  }

  action step = {
    nondet i = Set(1, 2).oneOf()
    nondet u = Units.oneOf()
    nondet t = Spellings.powerset().oneOf()
    nondet lvl = Set(1, 2).oneOf()
    nondet t1 = Spellings.oneOf()
    any { begin(i), commitFirstUpgrade(i), rederive(i), crash(i), previousReleaseEdits(u, t), removeThenReAdd(lvl, t1), saveTeamOnNewConnection(t) }
  }

  // Two racing instances never both migrate.
  val SingleStructure = structureRows <= 1
  // A unit reads only what its connection maps to its level.
  val SubsetWithinMapping = Units.forall(u => unitSubset.get(u).subseteq(mapping.get(LevelOf.get(u))))
  // After the first upgrade the migrator never changes the mapping (only System Admins add types).
  val MappingNeverGrowsFromUnits = structureRows == 0 or mapping == planned
  // A unit is marked as needing item types exactly when it reads none.
  val MarkMatchesEmptiness = structureRows == 0 or Units.forall(u => needsTypes.get(u) == (unitSubset.get(u).size() == 0))
  // At the first upgrade every unit keeps every type it read, ignoring case (a type both levels read sits on both).
  val UpgradeKeepsEveryType = structureRows == 0 or Units.forall(u => upgradeSubset.get(u) == upgradeOld.get(u))
  // A type removed from a level and re-added there is ticked again for no unit on that level the previous release did not edit.
  val ReAddTicksNoUnit = Units.forall(u => editedByPrevious.contains(u) or reAdded.forall(e => e._1 != LevelOf.get(u) or not(unitSubset.get(u).contains(e._2))))

  // A connection's mapping row, once created, holds today's create defaults (no type is added in feature A).
  val LateMappingIsDefaults = not(lateRow) or lateMapping == Defaults.get(1)
  // A Team on the new connection reads only what that connection maps.
  val LateSubsetWithinMapping = lateSubset.subseteq(lateMapping)
  // Saving a Team with today's defaults on a connection without a mapping row is accepted, as it is today.
  val LateDefaultsAccepted = lateOutcome != "refused" or not(FoldAll(lateRequested).subseteq(Defaults.get(1)))
}
