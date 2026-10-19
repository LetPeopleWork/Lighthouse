import {
	Button,
	Dialog,
	DialogActions,
	DialogContent,
	DialogContentText,
	DialogTitle,
} from "@mui/material";
import type React from "react";
import { useId, useState } from "react";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { useTerminology } from "../../../services/TerminologyContext";
import ActionButton from "../ActionButton/ActionButton";

/** What a changed default's followers read differently once they have been updated. */
export type WhatChanges = "parents" | "dependencies" | "sizes" | "owners";

/** Why the save asks before it updates anyone, and how many it would update. */
export type WhyUpdate =
	| { because: "fields"; teams: number; portfolios: number }
	| {
			because: "defaults";
			teams: number;
			portfolios: number;
			defaults: string[];
			changes: WhatChanges[];
	  }
	| {
			because: "estimation";
			teams: number;
			portfolios: number;
			defaults: number;
	  };

const counted = (count: number, singular: string, plural: string): string =>
	`${count} ${count === 1 ? singular : plural}`;

const joined = (parts: string[]): string => parts.join(" and ");

interface Words {
	team: string;
	teams: string;
	portfolio: string;
	portfolios: string;
	features: string;
}

const possessiveOfPlural = (plural: string): string =>
	plural.endsWith("s") ? `${plural}'` : `${plural}'s`;

const whenTheyChange = (changes: WhatChanges[], words: Words): string => {
	if (changes.length !== 1) {
		return "What they read changes only once they have been updated.";
	}
	const features = possessiveOfPlural(words.features);
	const sentences: Record<WhatChanges, string> = {
		parents: "Their parents change only once they have been updated.",
		dependencies: "Their dependencies change only once they have been updated.",
		sizes: `Their ${features} sizes change only once they have been updated.`,
		owners: `Their ${features} owners change only once they have been updated.`,
	};
	return sentences[changes[0]];
};

const followersOf = (why: WhyUpdate, words: Words): string => {
	const groups = [
		why.teams > 0 ? counted(why.teams, words.team, words.teams) : null,
		why.portfolios > 0
			? counted(why.portfolios, words.portfolio, words.portfolios)
			: null,
	].filter((group): group is string => group !== null);
	return joined(groups);
};

const whatTheQuestionSays = (why: WhyUpdate, words: Words): string => {
	if (why.because === "fields") {
		return `You changed the Additional Fields. Their values only fill in once each ${words.team} and ${words.portfolio} on this connection has been updated. Save only: they fill in with the next scheduled update instead.`;
	}
	const verb = why.teams + why.portfolios === 1 ? "follows" : "follow";
	if (why.because === "estimation") {
		const them = why.defaults === 1 ? "it" : "them";
		return `${followersOf(why, words)} ${verb} ${them}; they show the new estimation the next time their pages load.`;
	}
	const defaultWord = why.defaults.length === 1 ? "default" : "defaults";
	return `${followersOf(why, words)} ${verb} the ${joined(why.defaults)} ${defaultWord}. ${whenTheyChange(why.changes, words)} Save only: they change at their next scheduled update.`;
};

const titleOf = (why: WhyUpdate, words: Words): string => {
	if (why.because === "estimation") {
		const noun = why.defaults === 1 ? "default" : "defaults";
		return `Change the Estimation ${noun}?`;
	}
	return `Update ${counted(why.teams, words.team, words.teams)} and ${counted(why.portfolios, words.portfolio, words.portfolios)}?`;
};

interface UpdateQuestionProps {
	open: boolean;
	why: WhyUpdate;
	onUpdate: () => Promise<void>;
	onSaveOnly: () => Promise<void>;
	onCancel: () => void;
}

const UpdateQuestion: React.FC<UpdateQuestionProps> = ({
	open,
	why,
	onUpdate,
	onSaveOnly,
	onCancel,
}) => {
	const titleId = useId();
	const descriptionId = useId();
	const [saving, setSaving] = useState(false);
	const { getTerm } = useTerminology();
	const words: Words = {
		team: getTerm(TERMINOLOGY_KEYS.TEAM),
		teams: getTerm(TERMINOLOGY_KEYS.TEAMS),
		portfolio: getTerm(TERMINOLOGY_KEYS.PORTFOLIO),
		portfolios: getTerm(TERMINOLOGY_KEYS.PORTFOLIOS),
		features: getTerm(TERMINOLOGY_KEYS.FEATURES),
	};

	// An Estimation default is read when a page loads, so there is no update to offer, only the save.
	const asksToUpdate = why.because !== "estimation";

	const whileSaving = (answer: () => Promise<void>) => async () => {
		setSaving(true);
		try {
			await answer();
		} finally {
			setSaving(false);
		}
	};

	return (
		<Dialog
			open={open}
			onClose={saving ? undefined : onCancel}
			aria-labelledby={titleId}
			aria-describedby={descriptionId}
		>
			<DialogTitle id={titleId}>{titleOf(why, words)}</DialogTitle>
			<DialogContent>
				<DialogContentText id={descriptionId}>
					{whatTheQuestionSays(why, words)}
				</DialogContentText>
			</DialogContent>
			<DialogActions>
				<Button onClick={onCancel} color="primary" disabled={saving}>
					Cancel
				</Button>
				{asksToUpdate && (
					<ActionButton
						buttonText="Save only"
						onClickHandler={whileSaving(onSaveOnly)}
						buttonVariant="text"
						disabled={saving}
					/>
				)}
				<ActionButton
					buttonText={asksToUpdate ? "Save and update" : "Save"}
					onClickHandler={whileSaving(onUpdate)}
					buttonVariant="text"
					disabled={saving}
					autoFocus
				/>
			</DialogActions>
		</Dialog>
	);
};

export default UpdateQuestion;
