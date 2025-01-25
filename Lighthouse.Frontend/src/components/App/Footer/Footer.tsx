import CallIcon from "@mui/icons-material/Call";
import EmailIcon from "@mui/icons-material/Email";
import ForumIcon from "@mui/icons-material/Forum";
import LinkedInIcon from "@mui/icons-material/LinkedIn";
import { Box, Container, Typography } from "@mui/material";
import type React from "react";
import ExternalLinkButton from "../Header/ExternalLinkButton";
import LetPeopleWorkLogo from "../LetPeopleWork/LetPeopleWorkLogo";
import LighthouseVersion from "../LetPeopleWork/LighthouseVersion";

const Footer: React.FC = () => {
	return (
		<Box
			component="footer"
			className="footer"
			sx={{ backgroundColor: "white", py: 2 }}
		>
			<Container maxWidth={false}>
				<Box display="flex" justifyContent="space-between" alignItems="center">
					<Box>
						<LetPeopleWorkLogo />
					</Box>

					<Box textAlign="center">
						<Typography variant="body2">Contact us:</Typography>
						<Box>
							<ExternalLinkButton
								link="mailto:contact@letpeople.work"
								icon={EmailIcon}
								tooltip="Send an Email"
							/>
							<ExternalLinkButton
								link="https://calendly.com/letpeoplework/"
								icon={CallIcon}
								tooltip="Schedule a Call"
							/>
							<ExternalLinkButton
								link="https://www.linkedin.com/company/let-people-work/?viewAsMember=true"
								icon={LinkedInIcon}
								tooltip="View our LinkedIn Page"
							/>
							<ExternalLinkButton
								link="https://join.slack.com/t/let-people-work/shared_invite/zt-2y0zfim85-qhbgt8N0yw90G1P~JWXvlg"
								icon={ForumIcon}
								tooltip="Join our Slack Community"
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
