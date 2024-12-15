import React from 'react';
import { Dialog, DialogActions, DialogContent, DialogContentText, DialogTitle, Button, Box, Typography } from '@mui/material';
import Grid from '@mui/material/Grid2'
import { Link } from 'react-router-dom';
import LighthouseLogo from '../LetPeopleWork/LighthouseLogo';
import LetPeopleWorkLogo from '../LetPeopleWork/LetPeopleWorkLogo';

interface DemoDialogProps {
    open: boolean;
    onClose: () => void;
    onDontShowAgain: () => void;
}

const DemoDialog: React.FC<DemoDialogProps> = ({ open, onClose, onDontShowAgain }) => {
    return (
        <Dialog open={open} onClose={onClose}>
            <DialogTitle>
                <Grid container>
                    <Grid  size={{ xs: 6 }}>
                        <Box display="flex" justifyContent="flex-start">
                            <LighthouseLogo />
                        </Box>
                    </Grid>
                    <Grid  size={{ xs: 6 }}>
                        <Box display="flex" justifyContent="flex-end">
                            <LetPeopleWorkLogo />
                        </Box>
                    </Grid>
                </Grid>
                Demo Version
            </DialogTitle>
            <DialogContent>
                <DialogContentText>
                    <Typography variant='body1'>
                        This is a Demo Version of Lighthouse to show case how the tool works.
                        It's not using any real data and the forecasts you are getting are randomly generated. You can go through all the workflows, including creationg and editing of teams or projects, but nothing will be stored.                        
                    </Typography>
                    <Typography variant='body1' marginTop={2} marginBottom={2}>
                        You can still play around and see how you could use the tool.
                    </Typography>
                    <Typography variant='body1'>
                        Lighthouse is an open source application and provided free of charge. Go to <Link to={'https://github.com/LetPeopleWork/Lighthouse'}>GitHub</Link> to find instructions on how to run it yourself.
                        If you want more information, guidance on how to use the tool or would like to see a guided demo, reach out via <Link to={'https://letpeople.work'}>letpeople.work</Link>.
                    </Typography>
                </DialogContentText>
            </DialogContent>
            <DialogActions>
                <Button onClick={onClose} color="primary">
                    Close
                </Button>
                <Button onClick={onDontShowAgain} color="primary">
                    Don't show again
                </Button>
            </DialogActions>
        </Dialog>
    );
};

export default DemoDialog;
