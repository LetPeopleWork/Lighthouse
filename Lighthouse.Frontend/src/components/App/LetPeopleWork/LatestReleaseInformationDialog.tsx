import React from 'react';
import { Dialog, DialogContent, DialogActions, Button, Typography, Grid, List, ListItem, Link as MuiLink } from '@mui/material';
import { IRelease } from '../../../models/Release/Release';
import Markdown from 'react-markdown';
import remarkGfm from 'remark-gfm';

interface LatestReleaseInformationDialogProps {
    open: boolean;
    onClose: () => void;
    latestRelease: IRelease | null;
}

const LatestReleaseInformationDialog: React.FC<LatestReleaseInformationDialogProps> = ({ open, onClose, latestRelease }) => {
    return (
        <Dialog open={open} onClose={onClose} maxWidth="md" fullWidth>
            <DialogContent>
                {latestRelease && (
                    <Grid container spacing={3}>
                        <Grid item xs={12}>
                            <Typography variant='h4'>
                                <MuiLink href={latestRelease.link} target="_blank" rel="noopener noreferrer" sx={{ marginLeft: 1, fontWeight: 'bold' }}>
                                    {latestRelease.name}
                                </MuiLink>
                            </Typography>
                        </Grid>
                        <Grid item xs={12}>
                            <Markdown remarkPlugins={[remarkGfm]} >
                                {latestRelease.highlights}
                            </Markdown>
                        </Grid>
                        <Grid item xs={12}>
                            <Typography variant="h6" gutterBottom>Direct Downloads</Typography>
                            <List>
                                {latestRelease.assets.map((asset, index) => (
                                    <ListItem key={index} sx={{ padding: 0 }}>
                                        <MuiLink href={asset.link} target="_blank" rel="noopener noreferrer" sx={{ textDecoration: 'none', color: 'primary.main' }}>
                                            {asset.name}
                                        </MuiLink>
                                    </ListItem>
                                ))}
                            </List>
                        </Grid>
                    </Grid>
                )}
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
