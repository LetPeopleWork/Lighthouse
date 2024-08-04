import React from 'react';
import { Dialog, DialogContent, DialogActions, Button, Typography, Grid, List, ListItem, Link as MuiLink, DialogTitle } from '@mui/material';
import { ILighthouseRelease } from '../../../models/LighthouseRelease/LighthouseRelease';
import Markdown from 'react-markdown';
import remarkGfm from 'remark-gfm';
import InputGroup from '../../Common/InputGroup/InputGroup';
import GitHubIcon from '@mui/icons-material/GitHub';

interface LatestReleaseInformationDialogProps {
    open: boolean;
    onClose: () => void;
    newReleases: ILighthouseRelease[] | null;
}

const LatestReleaseInformationDialog: React.FC<LatestReleaseInformationDialogProps> = ({ open, onClose, newReleases }) => {
    return (
        <Dialog open={open} onClose={onClose} maxWidth="md" fullWidth>
            <DialogTitle>Newer Versions Available</DialogTitle>
            <DialogContent>
                {newReleases && newReleases.map((release, index) => (
                    <InputGroup key={index} title={release.name} initiallyExpanded={index == 0}>
                        <Grid item xs={12}>
                            <Typography variant='body2'>
                                <Markdown remarkPlugins={[remarkGfm]}>
                                    {release.highlights}
                                </Markdown>
                            </Typography>

                            <Typography variant='subtitle1'>Downloads:</Typography>
                            <List>
                                {release.assets.map((asset, index) => (
                                    <ListItem key={index} sx={{ padding: 0 }}>
                                        <MuiLink href={asset.link} target="_blank" rel="noopener noreferrer">
                                            {asset.name}
                                        </MuiLink>
                                    </ListItem>
                                ))}
                            </List>

                            <Typography variant='body1' marginTop={2}>
                                <GitHubIcon />
                                <MuiLink href={release.link} target="_blank" rel="noopener noreferrer" marginLeft={2}>
                                    See GitHub for more Details
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
