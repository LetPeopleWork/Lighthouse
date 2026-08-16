import Alert from "@mui/material/Alert";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import Chip from "@mui/material/Chip";
import Stack from "@mui/material/Stack";
import Table from "@mui/material/Table";
import TableBody from "@mui/material/TableBody";
import TableCell from "@mui/material/TableCell";
import TableContainer from "@mui/material/TableContainer";
import TableHead from "@mui/material/TableHead";
import TableRow from "@mui/material/TableRow";
import Typography from "@mui/material/Typography";
import type React from "react";
import { useCallback, useContext, useEffect, useState } from "react";
import InputGroup from "../../../components/Common/InputGroup/InputGroup";
import {
	type EncryptionKeyState,
	KEY_CUSTODY_WORDING,
	type KeyCustody,
} from "../../../models/Encryption/EncryptionKeyState";
import {
	SECRET_OUTCOME_WORDING,
	type SecretOutcomeNeedingAttention,
	type SecretReadabilityReport,
	type StoredSecret,
} from "../../../models/Encryption/SecretReadabilityReport";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";

// Where the key is not Lighthouse's to replace, the panel says who it belongs to and what that person
// does about it. It never draws a Rotate control that cannot be honoured - not even a disabled one,
// because a control that exists and cannot be used teaches the wrong model of who owns what.
const WHO_OWNS_THE_KEY: Record<KeyCustody, string> = {
	GeneratedForThisInstance:
		"Lighthouse made this key and keeps it, so it can make a new one for you.",
	SuppliedByConfiguration:
		"This key was supplied through the instance's configuration, so it belongs to whoever set it. To replace it, put the new key ahead of the old one in Encryption__Keys, start Lighthouse again, and then move the stored secrets onto it.",
	SuppliedByExternalSecret:
		"This key was supplied from a mounted secret, so it belongs to whoever keeps that secret. To replace it, put the new key ahead of the old one in that secret, restart the pod, and then move the stored secrets onto it.",
	NoDurableStore:
		"This instance has nowhere to keep a key that would still be there after a restart, so it is running on the key published with the product. Set Encryption__Key, or set Encryption__KeyStorePath to a directory that outlives this container, and start Lighthouse again.",
};

const KeyRing: React.FC<{ keyState: EncryptionKeyState }> = ({ keyState }) => (
	<TableContainer>
		<Table data-testid="encryption-key-ring">
			<TableBody>
				<TableRow>
					<TableCell>Key source</TableCell>
					<TableCell data-testid="encryption-custody">
						{KEY_CUSTODY_WORDING[keyState.custody]}
					</TableCell>
				</TableRow>
				<TableRow>
					<TableCell>Active key</TableCell>
					<TableCell data-testid="encryption-active-key-id">
						{keyState.activeKeyId}
					</TableCell>
				</TableRow>
				<TableRow>
					<TableCell>Keys held</TableCell>
					<TableCell>
						<Stack
							direction="row"
							spacing={1}
							useFlexGap
							sx={{ flexWrap: "wrap" }}
						>
							{keyState.keyIds.map((keyId) => (
								<Chip
									key={keyId}
									label={keyId}
									size="small"
									data-testid={`encryption-key-${keyId}`}
								/>
							))}
						</Stack>
					</TableCell>
				</TableRow>
				<TableRow>
					<TableCell>Kept in</TableCell>
					<TableCell data-testid="encryption-key-store-path">
						{keyState.keyStorePath}
					</TableCell>
				</TableRow>
			</TableBody>
		</Table>
	</TableContainer>
);

// Whether moving the stored secrets would achieve anything. The listed keys are the ones something is
// actually stored under, so any of them other than the key in force is work a move would do - which
// covers secrets left on the published key and secrets left on an earlier key without asking twice.
//
// Where the key in force IS the published key there is nowhere to move anything to: the move would
// re-encrypt that key onto itself, change nothing, and leave the warning standing. What fixes that
// instance is giving it a key of its own, which the custody sentence above the buttons already says.
const movingWouldAchieveSomething = (keyState: EncryptionKeyState) =>
	keyState.custody !== "NoDurableStore" &&
	keyState.keyIds.some((keyId) => keyId !== keyState.activeKeyId);

// A secret that moved needs no listing - the count already says so. What an operator has to act on is
// what was left behind, and the only useful thing to say about it is which Connection and which field.
const wasLeftBehind = (
	secret: StoredSecret,
): secret is StoredSecret & { outcome: SecretOutcomeNeedingAttention } =>
	secret.outcome === "CouldNotBeRead" ||
	secret.outcome === "CouldNotBeWritten" ||
	secret.outcome === "NotEncrypted";

// A check moves nothing, so a summary counting what moved would greet an operator with "Moved 0" on a
// perfectly healthy instance. The two actions answer different questions and are said in different words.
// Three, not two. Rotating and moving both write, but only one of them makes a key, and a rotation
// reported in the vocabulary of a move says nothing about the thing that actually happened.
type WhatWasAsked = "check" | "move" | "rotate";

const secrets = (count: number) =>
	count === 1 ? "1 stored secret" : `${count} stored secrets`;

// A count of zero is not information. Four categories of nothing compete with the one number that
// matters, and the number that matters is always one of the few that are not zero - so only those are
// said. An operator reads a count above zero as something they may have to do.
const whatEachOneIs = (report: SecretReadabilityReport) =>
	[
		[report.onActiveKeyCount, `on the key in force ${report.activeKeyId}`],
		[report.onRetiredKeyCount, "on an earlier key"],
		[report.plaintextCount, "never encrypted"],
		[report.unreadableCount, "could not be read"],
	]
		.filter(([count]) => (count as number) > 0)
		.map(([count, what]) => `${count} ${what}`)
		.join(", ");

const couldNotBeRead = (report: SecretReadabilityReport) =>
	report.unreadableCount > 0
		? ` ${secrets(report.unreadableCount)} could not be read.`
		: "";

// Rotation leads with the key it made, because that is what happened and it happens whether or not
// there was anything to move. On an empty instance this used to read "Moved 0 stored secrets onto key
// k-…", which is the one fact it did not report.
const summaryOf = (asked: WhatWasAsked, report: SecretReadabilityReport) => {
	if (asked === "rotate") {
		const moved =
			report.movedCount > 0
				? ` ${secrets(report.movedCount)} moved onto it.`
				: "";

		return `Made key ${report.activeKeyId} and put it in force.${moved}${couldNotBeRead(report)}`;
	}

	if (asked === "move") {
		return report.movedCount === 0
			? `Nothing needed moving - every stored secret was already on key ${report.activeKeyId}.${couldNotBeRead(report)}`
			: `Moved ${secrets(report.movedCount)} onto key ${report.activeKeyId}.${couldNotBeRead(report)}`;
	}

	return report.secrets.length === 0
		? "Checked every stored secret. There are none yet."
		: `Checked ${secrets(report.secrets.length)}: ${whatEachOneIs(report)}.`;
};

const WhatHappened: React.FC<{
	asked: WhatWasAsked;
	report: SecretReadabilityReport;
}> = ({ asked, report }) => {
	const leftBehind = report.secrets.filter(wasLeftBehind);

	return (
		<Box sx={{ mt: 2 }} data-testid="encryption-report">
			<Alert severity={leftBehind.length > 0 ? "warning" : "success"}>
				{summaryOf(asked, report)}
			</Alert>

			{leftBehind.length > 0 && (
				<TableContainer sx={{ mt: 2 }}>
					<Table size="small" data-testid="encryption-report-secrets">
						<TableHead>
							<TableRow>
								<TableCell>Connection</TableCell>
								<TableCell>Field</TableCell>
								<TableCell>What happened</TableCell>
							</TableRow>
						</TableHead>
						<TableBody>
							{leftBehind.map((secret) => (
								<TableRow key={`${secret.connectionId}-${secret.field}`}>
									<TableCell>{secret.connectionName}</TableCell>
									<TableCell>{secret.field}</TableCell>
									<TableCell>
										{SECRET_OUTCOME_WORDING[secret.outcome]}
									</TableCell>
								</TableRow>
							))}
						</TableBody>
					</Table>
				</TableContainer>
			)}
		</Box>
	);
};

const EncryptionPanel: React.FC = () => {
	const [keyState, setKeyState] = useState<EncryptionKeyState | null>(null);
	const [report, setReport] = useState<SecretReadabilityReport | null>(null);
	const [asked, setAsked] = useState<WhatWasAsked>("move");
	const [failure, setFailure] = useState<string | null>(null);
	const [busy, setBusy] = useState(false);

	const { encryptionService } = useContext(ApiServiceContext);

	const readKeyState = useCallback(async () => {
		try {
			setKeyState(await encryptionService.getKeyState());
		} catch {
			setKeyState(null);
		}
	}, [encryptionService]);

	useEffect(() => {
		readKeyState();
	}, [readKeyState]);

	const run = async (
		whatWasAsked: WhatWasAsked,
		action: () => Promise<SecretReadabilityReport>,
	): Promise<void> => {
		setBusy(true);
		setFailure(null);

		try {
			setReport(await action());
			setAsked(whatWasAsked);
			await readKeyState();
		} catch (error_) {
			setFailure(
				error_ instanceof Error
					? error_.message
					: "The stored secrets could not be moved.",
			);
		} finally {
			setBusy(false);
		}
	};

	const moveThemOntoTheActiveKey = () =>
		run("move", () => encryptionService.reEncryptSecrets());

	if (keyState === null) {
		return null;
	}

	return (
		<InputGroup title="Secret Encryption Key" initiallyExpanded={true}>
			<KeyRing keyState={keyState} />

			<Typography
				variant="body2"
				sx={{ mt: 2 }}
				data-testid="encryption-custody-explanation"
			>
				{WHO_OWNS_THE_KEY[keyState.custody]}
			</Typography>

			{keyState.allowsStartWithUnreadableSecrets && (
				<Alert
					severity="error"
					sx={{ mt: 2 }}
					data-testid="started-past-the-refusal-notice"
				>
					{
						"This instance was started with Encryption__StartEvenIfNothingStoredCanBeRead set, so it is running with stored credentials it cannot read. Press Check secrets below for the Connection and field each one sits in, enter those credentials again, then remove the setting and restart."
					}
				</Alert>
			)}

			{keyState.secretsUnderPublishedKey > 0 && (
				<Alert
					severity="warning"
					sx={{ mt: 2 }}
					data-testid="published-key-notice"
				>
					{`${keyState.secretsUnderPublishedKey} stored credentials are still readable with the key published with Lighthouse, which anyone who has a copy of Lighthouse can obtain. Moving them onto this instance's own key is the fix, and nobody has to re-enter anything.`}
				</Alert>
			)}

			<Stack direction="row" spacing={2} sx={{ mt: 2 }}>
				{keyState.canMint && (
					<Button
						variant="outlined"
						disabled={busy}
						data-testid="rotate-key-button"
						onClick={() => run("rotate", () => encryptionService.rotateKey())}
					>
						Rotate key
					</Button>
				)}
				{movingWouldAchieveSomething(keyState) && (
					<Button
						variant={
							keyState.secretsUnderPublishedKey > 0 ? "contained" : "outlined"
						}
						disabled={busy}
						data-testid="reencrypt-button"
						onClick={moveThemOntoTheActiveKey}
					>
						Move stored secrets
					</Button>
				)}
				<Button
					variant="text"
					disabled={busy}
					data-testid="check-secrets-button"
					onClick={() => run("check", () => encryptionService.checkSecrets())}
				>
					Check secrets
				</Button>
			</Stack>

			{failure !== null && (
				<Alert severity="error" sx={{ mt: 2 }} data-testid="encryption-failure">
					{failure}
				</Alert>
			)}

			{report !== null && <WhatHappened asked={asked} report={report} />}
		</InputGroup>
	);
};

export default EncryptionPanel;
