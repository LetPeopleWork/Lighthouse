import CallIcon from "@mui/icons-material/Call";
import EmailIcon from "@mui/icons-material/Email";
import ForumIcon from "@mui/icons-material/Forum";
import LinkedInIcon from "@mui/icons-material/LinkedIn";
import ShoppingCartIcon from "@mui/icons-material/ShoppingCart";
import VolunteerActivismIcon from "@mui/icons-material/VolunteerActivism";
import {
	Box,
	Container,
	Typography,
	useMediaQuery,
	useTheme,
} from "@mui/material";
import type React from "react";
import ExternalLinkButton from "../Header/ExternalLinkButton";
import LetPeopleWorkLogo from "../LetPeopleWork/LetPeopleWorkLogo";
import LighthouseVersion from "../LetPeopleWork/LighthouseVersion";

const Footer: React.FC = () => {
	const theme = useTheme();
	const isMobile = useMediaQuery(theme.breakpoints.down("sm"));

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
					display="flex"
					flexDirection={isMobile ? "column" : "row"}
					justifyContent="space-between"
					alignItems={isMobile ? "center" : "flex-start"}
					gap={isMobile ? 2 : 0}
				>
					<Box>
						<LetPeopleWorkLogo />
					</Box>

					<Box textAlign="center">
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
								link="https://calendly.com/let-people-work"
								icon={CallIcon}
								tooltip="Schedule a Call"
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
								link="https://miro.com/app/board/uXjVItmYi5c=/?share_link_id=330450244368"
								icon={ShoppingCartIcon}
								tooltip="See our Offering"
							/>
							<ExternalLinkButton
								link="https://ko-fi.com/letpeoplework"
								icon={VolunteerActivismIcon}
								tooltip="Support Our Work"
							/>
						</Box>
					</Box>

					<Box textAlign="right">
						<LighthouseVersion />
					</Box>
				</Box>
			</Container>
		</Box>
	);
};

export default Footer;
