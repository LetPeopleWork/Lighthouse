import BugReportIcon from "@mui/icons-material/BugReport";
import SlackIcon from "@mui/icons-material/Chat";
import EmailIcon from "@mui/icons-material/Email";
import HandshakeIcon from "@mui/icons-material/Handshake";
import VolunteerActivismIcon from "@mui/icons-material/VolunteerActivism";
import {
	Box,
	Button,
	Dialog,
	DialogActions,
	DialogContent,
	DialogContentText,
	DialogTitle,
	Link,
	Typography,
} from "@mui/material";
import type React from "react";

interface FeedbackDialogProps {
	open: boolean;
	onClose: () => void;
}

const FeedbackDialog: React.FC<FeedbackDialogProps> = ({ open, onClose }) => {
	return (
		<Dialog open={open} onClose={onClose} maxWidth="md" fullWidth>
			<DialogTitle>We'd Love to Hear from You!</DialogTitle>
			<DialogContent>
				<DialogContentText component="div">
					<Typography variant="body1" sx={{ mb: 2 }}>
						We are eager to get feedback and constantly improve Lighthouse to
						better serve your needs.
					</Typography>

					<Typography variant="body1" sx={{ mb: 2 }}>
						We run regular reviews and would love for people to join there to
						hear their thoughts and discuss improvements.
					</Typography>

					<Box
						sx={{ display: "flex", alignItems: "center", gap: 1, mt: 3, mb: 2 }}
					>
						<SlackIcon color="primary" />
						<Typography variant="h6" component="h3">
							Preferred Way: Join Our Slack Community
						</Typography>
					</Box>
					<Typography variant="body1" sx={{ mb: 2 }}>
						Our preferred way of getting feedback is through our Slack Channel
						where you can engage with the community and development team:
					</Typography>
					<Box sx={{ pl: 2, mb: 2 }}>
						<Link
							href="https://join.slack.com/t/let-people-work/shared_invite/zt-38df4z4sy-iqJEo6S8kmIgIfsgsV0J1A"
							target="_blank"
							rel="noopener noreferrer"
							variant="body1"
						>
							Join Let People Work Slack Community
						</Link>
					</Box>

					<Box
						sx={{ display: "flex", alignItems: "center", gap: 1, mt: 3, mb: 2 }}
					>
						<EmailIcon color="primary" />
						<Typography variant="h6" component="h3">
							Alternative: Email Feedback
						</Typography>
					</Box>
					<Typography variant="body1" sx={{ mb: 2 }}>
						You can also provide feedback via email to{" "}
						<Link
							href="mailto:lighthouse@letpeople.work"
							variant="body1"
							color="primary"
						>
							lighthouse@letpeople.work
						</Link>
					</Typography>

					<Box
						sx={{ display: "flex", alignItems: "center", gap: 1, mt: 2, mb: 1 }}
					>
						<BugReportIcon color="primary" fontSize="small" />
						<Typography variant="body1" sx={{ mb: 1 }}>
							When reporting issues, please include:
						</Typography>
					</Box>
					<Box sx={{ pl: 2, mb: 2 }}>
						<Typography variant="body2" component="div">
							<strong>For Feature Requests:</strong>
							<br />• Description of the desired functionality
							<br />• Use case and expected benefits
							<br />• Any relevant context or examples
						</Typography>
						<Typography variant="body2" component="div" sx={{ mt: 2 }}>
							<strong>For Bugs:</strong>
							<br />• Steps to reproduce the issue
							<br />• Lighthouse version you're using
							<br />• Expected behavior vs. actual behavior
							<br />• Screenshots or logs if applicable
						</Typography>
					</Box>

					<Box
						sx={{ display: "flex", alignItems: "center", gap: 1, mt: 3, mb: 2 }}
					>
						<VolunteerActivismIcon color="primary" />
						<Typography variant="h6" component="h3">
							Support Our Work
						</Typography>
					</Box>
					<Typography variant="body1" sx={{ mb: 2 }}>
						Lighthouse is completely Free and Open Source. If you find it
						valuable for your team and would like to support its continued
						development, consider making a donation:
					</Typography>
					<Box sx={{ pl: 2, mb: 2 }}>
						<Link
							href="https://ko-fi.com/letpeoplework"
							target="_blank"
							rel="noopener noreferrer"
							variant="body1"
						>
							Support us on Ko-fi
						</Link>
					</Box>

					<Box
						sx={{ display: "flex", alignItems: "center", gap: 1, mt: 3, mb: 2 }}
					>
						<HandshakeIcon color="primary" />
						<Typography variant="h6" component="h3">
							Sponsored Feature Development
						</Typography>
					</Box>
					<Typography variant="body1" sx={{ mb: 1 }}>
						Looking for a specific feature that would make Lighthouse perfect
						for your team? We'd love to help bring your vision to life!
					</Typography>
					<Typography variant="body1">
						While Lighthouse remains completely Free and Open Source, we offer
						custom feature development services. Whether it's a unique
						integration, specialized reporting, or workflow enhancements - let's
						discuss how we can build exactly what you need. Reach out through
						the email above to explore the possibilities!
					</Typography>
				</DialogContentText>
			</DialogContent>
			<DialogActions>
				<Button onClick={onClose} color="primary" variant="contained">
					Close
				</Button>
			</DialogActions>
		</Dialog>
	);
};

export default FeedbackDialog;
