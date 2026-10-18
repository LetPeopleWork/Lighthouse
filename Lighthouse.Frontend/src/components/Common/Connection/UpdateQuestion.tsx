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

/** Why the save asks before it updates anyone, and how many it would update. */
export type WhyUpdate =
	| { because: "fields"; teams: number; portfolios: number }
	| {
			because: "defaults";
			teams: number;
			portfolios: number;
			defaults: string[];
	  };

const counted = (count: number, singular: string, plural: string): string =>
	`${count} ${count === 1 ? singular : plural}`;

const joined = (parts: string[]): string => parts.join(" and ");

interface Words {
	team: string;
	teams: string;
	portfolio: string;
	portfolios: string;
}

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
	const defaultWord = why.defaults.length === 1 ? "default" : "defaults";
	return `${followersOf(why, words)} ${verb} the ${joined(why.defaults)} ${defaultWord}. Their parents change only once they have been updated. Save only: they change at their next scheduled update.`;
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
	};

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
			<DialogTitle id={titleId}>
				{`Update ${counted(why.teams, words.team, words.teams)} and ${counted(why.portfolios, words.portfolio, words.portfolios)}?`}
			</DialogTitle>
			<DialogContent>
				<DialogContentText id={descriptionId}>
					{whatTheQuestionSays(why, words)}
				</DialogContentText>
			</DialogContent>
			<DialogActions>
				<Button onClick={onCancel} color="primary" disabled={saving}>
					Cancel
				</Button>
				<ActionButton
					buttonText="Save only"
					onClickHandler={whileSaving(onSaveOnly)}
					buttonVariant="text"
					disabled={saving}
				/>
				<ActionButton
					buttonText="Save and update"
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
