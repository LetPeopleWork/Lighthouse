import { Box, Container, Typography } from "@mui/material";
import type React from "react";
import LetPeopleWorkLogo from "../LetPeopleWork/LetPeopleWorkLogo";
import LighthouseVersion from "../LetPeopleWork/LighthouseVersion";

import CallIcon from "@mui/icons-material/Call";
import EmailIcon from "@mui/icons-material/Email";
import GitHubIcon from "@mui/icons-material/GitHub";
import LinkedInIcon from "@mui/icons-material/LinkedIn";
import ExternalLinkButton from "../Header/ExternalLinkButton";

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
								link="https://github.com/LetPeopleWork/Lighthouse/issues"
								icon={GitHubIcon}
								tooltip="Raise an Issue on GitHub"
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
