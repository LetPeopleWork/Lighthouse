import EmailIcon from "@mui/icons-material/Email";
import ForumIcon from "@mui/icons-material/Forum";
import LinkedInIcon from "@mui/icons-material/LinkedIn";
import TipsAndUpdatesIcon from "@mui/icons-material/TipsAndUpdates";
import VolunteerActivismIcon from "@mui/icons-material/VolunteerActivism";
import {
	Box,
	Container,
	Typography,
	useMediaQuery,
	useTheme,
} from "@mui/material";
import type React from "react";
import { useUsageDataConsent } from "../../../hooks/useUsageDataConsent";
import {
	USAGE_DATA_COLLECTOR_NAME,
	USAGE_DATA_DOCS_URL,
	USAGE_DATA_NEVER_SENT,
	USAGE_DATA_RESIDENCY,
	USAGE_DATA_SENT_FIELDS,
} from "../../../models/UsageData/UsageData";
import { UsageDataDialog } from "../../UsageData/UsageDataDialog";
import { UsageDataIndicator } from "../../UsageData/UsageDataIndicator";
import ExternalLinkButton from "../Header/ExternalLinkButton";
import LetPeopleWorkLogo from "../LetPeopleWork/LetPeopleWorkLogo";
import LighthouseVersion from "../LetPeopleWork/LighthouseVersion";

const Footer: React.FC = () => {
	const theme = useTheme();
	const isMobile = useMediaQuery(theme.breakpoints.down("sm"));
	const usageData = useUsageDataConsent();

	return (
		<Box
			component="footer"
			className="footer"
			sx={{
				backgroundColor: theme.palette.background.paper,
				py: 2,
				borderTop: `1px solid ${theme.palette.divider}`,
				mt: "auto",
				transition: "background-color 0.3s ease",
			}}
		>
			<Container maxWidth={false}>
				<Box
					sx={{
						display: "flex",
						flexDirection: isMobile ? "column" : "row",
						justifyContent: "space-between",
						alignItems: isMobile ? "center" : "flex-start",
						gap: isMobile ? 2 : 0,
					}}
				>
					<Box>
						<LetPeopleWorkLogo />
					</Box>

					<Box sx={{ textAlign: "center" }}>
						<Typography variant="body2" sx={{ mb: 1 }}>
							Contact us:
						</Typography>
						<Box sx={{ display: "flex", justifyContent: "center", gap: 1 }}>
							<ExternalLinkButton
								link="mailto:contact@letpeople.work"
								icon={EmailIcon}
								tooltip="Send an Email"
							/>
							<ExternalLinkButton
								link="https://www.linkedin.com/company/let-people-work/?viewAsMember=true"
								icon={LinkedInIcon}
								tooltip="View our LinkedIn Page"
							/>
							<ExternalLinkButton
								link="https://join.slack.com/t/let-people-work/shared_invite/zt-38df4z4sy-iqJEo6S8kmIgIfsgsV0J1A"
								icon={ForumIcon}
								tooltip="Join our Slack Community"
							/>
							<ExternalLinkButton
								link="https://ideas.letpeople.work"
								icon={TipsAndUpdatesIcon}
								tooltip="Share Feature Requests on our Product Board"
							/>
							<ExternalLinkButton
								link="https://ko-fi.com/letpeoplework"
								icon={VolunteerActivismIcon}
								tooltip="Support Our Work"
							/>
						</Box>
					</Box>

					<Box
						sx={{
							textAlign: "right",
							display: "flex",
							alignItems: "center",
							gap: 0.5,
						}}
					>
						{/* Beside the version, and shown whatever the answer is - an indicator that
						    appears only while sending would answer nothing by its absence. */}
						<UsageDataIndicator
							state={usageData.indicatorState}
							onOpenDecision={usageData.openDialog}
						/>
						<LighthouseVersion />
					</Box>

					<UsageDataDialog
						open={usageData.isDialogOpen}
						collectorName={USAGE_DATA_COLLECTOR_NAME}
						dataResidency={USAGE_DATA_RESIDENCY}
						fields={USAGE_DATA_SENT_FIELDS}
						neverSent={USAGE_DATA_NEVER_SENT}
						docsUrl={USAGE_DATA_DOCS_URL}
						willAskAgain={usageData.willAskAgain}
						onDecision={usageData.decide}
						onClose={usageData.closeDialog}
					/>
				</Box>
			</Container>
		</Box>
	);
};

export default Footer;
