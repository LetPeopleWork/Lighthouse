import { Link, Typography } from "@mui/material";
import type React from "react";
import type { IWorkTrackingSystemOption } from "../../../models/WorkTracking/WorkTrackingSystemOption";

export const SECRET_HANDLING_NOTICE_TEST_ID = "secret-handling-notice";

export const SECRET_HANDLING_DOCS_URL =
	"https://docs.lighthouse.letpeople.work/security.html";

// Every claim below has to hold on every install, including one still running the published
// default encryption key. That is why it promises nothing about the key itself, and why it reads
// as an answer rather than an alarm: the person pasting a token is rarely the person who can
// configure a key, so a warning here would frighten without offering any way to act on it.
const NOTICE_COPY =
	"Secrets you enter here are encrypted before they are saved, are never shown again — not even to an administrator — and never leave this instance. You can revoke one wherever you created it to cut off access immediately.";

const LINK_COPY = "How Lighthouse protects your credentials";

export const containsSecretField = (
	options: IWorkTrackingSystemOption[],
): boolean => options.some((option) => option.isSecret);

interface SecretHandlingNoticeProps {
	docsUrl?: string;
}

const SecretHandlingNotice: React.FC<SecretHandlingNoticeProps> = ({
	docsUrl = SECRET_HANDLING_DOCS_URL,
}) => (
	<Typography
		variant="body2"
		color="text.secondary"
		data-testid={SECRET_HANDLING_NOTICE_TEST_ID}
	>
		{NOTICE_COPY}{" "}
		<Link href={docsUrl} target="_blank" rel="noopener noreferrer">
			{LINK_COPY}
		</Link>
	</Typography>
);

export default SecretHandlingNotice;
