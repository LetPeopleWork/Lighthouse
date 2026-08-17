Feature: The key the cluster owns arrives as a file (Epic 5775, Slice 05 — US-05)
  As an operator whose own secret store owns every other credential this installation holds
  I want the encryption key to come from a secret that store populates, and to reach the container as a file
  So that no two installs share a key, and the key cannot be read out of a process dump

  # Every other credential this chart hands the container travels as an environment variable, and for
  # the encryption key that would be wrong twice over. An environment variable is readable in a process
  # dump, so it fails the one thing this key is being moved for; and it cannot change under a running
  # process, so an operator who adds a key would have to roll the pod to be noticed. The key therefore
  # travels as a mounted file, and the divergence from how the database password travels is the point
  # rather than an inconsistency to tidy up later.
  #
  # The file is mounted readable by everyone in the container rather than by its owner alone: the
  # runtime user is not root, the projected file is owned by root, and there is nothing in this chart
  # to bridge that. At owner-only the application cannot open its own keys and the pod never starts.
  # The trust boundary is the pod, which has exactly one process, not the file mode.

  @driving_port @us-05 @slice-05 @real-io
  Scenario: A secret an external store owns becomes the keys the instance reads
    Given an operator whose external store populates a secret holding the keys
    When they install naming that secret
    Then the chart renders no encryption secret of its own
    And the container reads the keys from the secret the operator named
    And the instance reports the key as one an external secret supplied

  @driving_port @us-05 @slice-05
  Scenario: A key given on the command line is kept in the release's own secret
    Given an operator who supplies a key directly rather than naming a secret
    When they install
    Then the release carries a secret of its own holding that key
    And the instance reports the key as one its configuration supplied

  @driving_port @us-05 @slice-05 @real-io
  Scenario: The keys arrive as a file the container may only read
    Given an install in either custody
    When the container is described
    Then the keys are mounted as a file, at a path the configuration names
    And the mount is read-only, because nothing in this instance ever writes a key
    And the file is readable by the user the application runs as, which is not root

  @us-05 @slice-05 @property @error
  Scenario: No key is anywhere in the container's environment
    Given an install in either custody
    When every environment variable the container is given is read
    Then none of them is key material
    And the only one whose name mentions the keys carries the path to the file, which is not a secret

  @us-05 @slice-05 @property @error
  Scenario: No key is anywhere in the non-secret configuration
    Given an install in either custody
    When the configuration the chart writes in the open is read
    Then it holds the path to the file and nothing else about the keys
    And the path is an absolute one, so it cannot be mistaken for material

  @us-05 @slice-05 @edge
  Scenario: Adding a key to the secret does not roll the pod
    Given a tenant whose only externally-owned credential is the encryption secret
    When the manifests are rendered
    Then nothing asks for the pod to be restarted when that secret changes
    And nothing does, because the instance notices a new key by reading the file again rather than by being restarted

  @us-05 @slice-05 @error @real-io
  Scenario: A named secret that is not there leaves the pod unstarted
    Given an install naming a secret that does not exist
    When it is applied
    Then the pod does not start, and says which secret it is waiting for
    And it never starts without keys, because that is how an instance quietly comes up on the published one

  @us-05 @slice-05 @edge
  Scenario: The database credential and the sign-in secret travel exactly as they did
    Given an install that carries all three secrets
    When the manifests are rendered
    Then the database credential and the sign-in secret reach the container the way they always have
    And only the encryption key travels the new way

  @walking_skeleton @driving_port @us-05 @slice-05 @real-io
  Scenario: A real cluster projects the file and the instance comes up on it
    Given a real cluster and a secret an operator created in it
    When Lighthouse is installed naming that secret and waited for
    Then it becomes ready, serves its health check, and shows its own page
    And the key it reports is the one out of the operator's secret, not the published one
