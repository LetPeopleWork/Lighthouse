import React from 'react';
import { Dialog, DialogContent, DialogActions, Button, Typography, List, ListItem, Link as MuiLink, DialogTitle, Box } from '@mui/material';
import Grid from '@mui/material/Grid2'
import { ILighthouseRelease } from '../../../models/LighthouseRelease/LighthouseRelease';
import Markdown from 'react-markdown';
import remarkGfm from 'remark-gfm';
import InputGroup from '../../Common/InputGroup/InputGroup';
import GitHubIcon from '@mui/icons-material/GitHub';
import DownloadIcon from '@mui/icons-material/Download';
import UpdateIcon from '@mui/icons-material/Update';

interface LatestReleaseInformationDialogProps {
    open: boolean;
    onClose: () => void;
    newReleases: ILighthouseRelease[] | null;
}

const LatestReleaseInformationDialog: React.FC<LatestReleaseInformationDialogProps> = ({ open, onClose, newReleases }) => {
    return (
        <Dialog open={open} onClose={onClose} maxWidth="md" fullWidth>
            <DialogTitle>Update Available</DialogTitle>
            <DialogContent>
                <Box display="flex" alignItems="center" mb={2}>
                    <DownloadIcon color="primary" sx={{ marginRight: 1 }} />
                    <Typography variant="body1">
                        To update Lighthouse, stop the running instance, download the latest version, and extract it into your Lighthouse folder. Your database and configuration will not be changed by updating.
                    </Typography>
                </Box>
                
                <Box display="flex" alignItems="center" mb={3}>
                    <UpdateIcon color="secondary" sx={{ marginRight: 1 }} />
                    <Typography variant="body1">
                        You can also use the update script for your respective operating system in your Lighthouse folder (for example, <code>update_windows.ps1</code>). Running this will automatically download and extract the latest version for you.
                    </Typography>
                </Box>

                {newReleases?.map((release, index) => (
                    <InputGroup key={release.name} title={release.name} initiallyExpanded={index === 0}>
                        <Grid  size={{ xs: 12 }}>
                            <Typography variant='body2'>
                                <Markdown remarkPlugins={[remarkGfm]}>
                                    {release.highlights}
                                </Markdown>
                            </Typography>

                            <Typography variant='subtitle1'>Downloads:</Typography>
                            <List>
                                {release.assets.map((asset) => (
                                    <ListItem key={asset.name} sx={{ padding: 0 }}>
                                        <MuiLink href={asset.link} target="_blank" rel="noopener noreferrer">
                                            {asset.name}
                                        </MuiLink>
                                    </ListItem>
                                ))}
                            </List>

                            <Typography variant='body1' marginTop={2}>
                                <GitHubIcon />
                                <MuiLink href={release.link} target="_blank" rel="noopener noreferrer" marginLeft={2}>
                                    See GitHub for more details
                                </MuiLink>
                            </Typography>
                        </Grid>
                    </InputGroup>
                ))}
            </DialogContent>
            <DialogActions>
                <Button onClick={onClose} color="primary" variant="contained">
                    Close
                </Button>
            </DialogActions>
        </Dialog>
    );
};

export default LatestReleaseInformationDialog;
