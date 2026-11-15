// The Linux sign step inside the signing-runner container (tools/codesign/sign-windows.sh).
// One sequential script: start pcscd, wait a bounded time for the YubiKey reader,
// sign each file with osslsigncode (PIN on stdin), stop pcscd on every exit (trap).
// The environment may plug/unplug the key at any time and the runner may cancel the job.
module sign_lifecycle {
  pure val MAX_WAIT = 3
  pure val FILES = 3
  pure val STEP_BOUND = MAX_WAIT + FILES + 3

  type Phase = Idle | DaemonStarted | ReaderFound | Signed | Failed

  var phase: Phase
  var pcscd: bool
  var keyPresent: bool
  var waited: int
  var signedCount: int
  var pinSent: bool
  var readerSeen: bool
  var plugInMessage: bool
  var steps: int

  pure def terminal(p: Phase): bool = match p { | Signed => true | Failed => true | _ => false }

  action init = all {
    phase' = Idle,
    pcscd' = false,
    nondet k = Set(true, false).oneOf()
    keyPresent' = k,
    waited' = 0,
    signedCount' = 0,
    pinSent' = false,
    readerSeen' = false,
    plugInMessage' = false,
    steps' = 0,
  }

  action startDaemon = all {
    phase == Idle,
    phase' = DaemonStarted, pcscd' = true, keyPresent' = keyPresent,
    waited' = waited, signedCount' = signedCount, pinSent' = pinSent, readerSeen' = readerSeen,
    plugInMessage' = plugInMessage, steps' = steps + 1,
  }

  action readerAppears = all {
    phase == DaemonStarted, keyPresent,
    phase' = ReaderFound, readerSeen' = true, pcscd' = pcscd, keyPresent' = keyPresent,
    waited' = waited, signedCount' = signedCount, pinSent' = pinSent,
    plugInMessage' = plugInMessage, steps' = steps + 1,
  }

  action waitTick = all {
    phase == DaemonStarted, not(keyPresent), waited < MAX_WAIT, // MUTANT-M3
    waited' = waited + 1, phase' = phase, pcscd' = pcscd, keyPresent' = keyPresent,
    signedCount' = signedCount, pinSent' = pinSent, readerSeen' = readerSeen,
    plugInMessage' = plugInMessage, steps' = steps + 1,
  }

  action giveUp = all {
    phase == DaemonStarted, not(keyPresent), waited == MAX_WAIT,
    phase' = Failed, plugInMessage' = true,
    pcscd' = false, // MUTANT-M2
    keyPresent' = keyPresent, waited' = waited, signedCount' = signedCount,
    pinSent' = pinSent, readerSeen' = readerSeen, steps' = steps + 1,
  }

  action signOne = all {
    phase == ReaderFound, // MUTANT-M1
    signedCount < FILES,
    pinSent' = true, keyPresent' = keyPresent, waited' = waited, readerSeen' = readerSeen,
    plugInMessage' = plugInMessage, steps' = steps + 1,
    if (keyPresent) all { signedCount' = signedCount + 1, phase' = phase, pcscd' = pcscd }
    else all { signedCount' = signedCount, phase' = Failed, pcscd' = false },
  }

  action finish = all {
    phase == ReaderFound,
    signedCount == FILES, // MUTANT-M5
    phase' = Signed, pcscd' = false, keyPresent' = keyPresent, waited' = waited,
    signedCount' = signedCount, pinSent' = pinSent, readerSeen' = readerSeen,
    plugInMessage' = plugInMessage, steps' = steps + 1,
  }

  action cancel = all {
    not(terminal(phase)), phase != Idle,
    phase' = Failed, pcscd' = false, keyPresent' = keyPresent, waited' = waited,
    signedCount' = signedCount, pinSent' = pinSent, readerSeen' = readerSeen,
    plugInMessage' = plugInMessage, steps' = steps + 1,
  }

  action toggleKey = all {
    not(terminal(phase)),
    keyPresent' = not(keyPresent),
    phase' = phase, pcscd' = pcscd, waited' = waited, signedCount' = signedCount,
    pinSent' = pinSent, readerSeen' = readerSeen, plugInMessage' = plugInMessage, steps' = steps,
  }

  action stutterDone = all {
    terminal(phase),
    keyPresent' = keyPresent,
    phase' = phase, pcscd' = pcscd, waited' = waited, signedCount' = signedCount,
    pinSent' = pinSent, readerSeen' = readerSeen, plugInMessage' = plugInMessage, steps' = steps,
  }

  action step = any { startDaemon, readerAppears, waitTick, giveUp, signOne, finish, cancel, toggleKey, stutterDone }

  val noPinWithoutReader = pinSent implies readerSeen
  val plugInMessageBeforeAnyPin = plugInMessage implies not(pinSent)
  val daemonStoppedWhenDone = terminal(phase) implies not(pcscd)
  val waitIsBounded = waited <= MAX_WAIT
  val signedMeansAllFiles = phase == Signed implies signedCount == FILES
  val protocolStepsBounded = steps <= STEP_BOUND
  val safety = noPinWithoutReader and plugInMessageBeforeAnyPin and daemonStoppedWhenDone
    and waitIsBounded and signedMeansAllFiles and protocolStepsBounded

  // Inhabitation witnesses: each is expected to be violated, which shows the state is reachable.
  val witnessNeverSigned = phase != Signed
  val witnessNeverPlugInMessage = not(plugInMessage)
}
