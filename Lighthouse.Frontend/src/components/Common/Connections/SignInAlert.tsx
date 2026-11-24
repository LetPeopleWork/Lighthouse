import { Alert, Button } from "@mui/material";
import type { OAuthSignInMessage } from "../../../hooks/useOAuthSignIn";

interface SignInAlertProps {
	message: string;
	actionLabel: string;
	onAction: () => void;
	isSigningIn: boolean;
	inlineMessage: OAuthSignInMessage | null;
	inlineMessageTestId: string;
}

/** A warning whose button starts a sign-in, with the outcome of a sign-in that did not complete below it. */
const SignInAlert = ({
	message,
	actionLabel,
	onAction,
	isSigningIn,
	inlineMessage,
	inlineMessageTestId,
}: SignInAlertProps) => (
	<>
		<Alert
			severity="warning"
			action={
				<Button
					color="inherit"
					size="small"
					onClick={onAction}
					disabled={isSigningIn}
				>
					{actionLabel}
				</Button>
			}
		>
			{message}
		</Alert>
		{inlineMessage !== null && (
			<Alert
				severity={inlineMessage.severity}
				data-testid={inlineMessageTestId}
			>
				{inlineMessage.text}
			</Alert>
		)}
	</>
);

export default SignInAlert;
