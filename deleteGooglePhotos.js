let idsToDelete = [
  
]

const baseUrl = 'https://photos.google.com/photo/'

const QUERY_SELECTORS = {
  deleteButton: 'button[aria-label="Delete"]',
  confirmationButton: '#yDmH0d > div.VfPpkd-Sx9Kwc.cC1eCc.UDxLd.PzCPDd.V639qd.oEOLpc.A9Uzve.VfPpkd-Sx9Kwc-OWXEXe-FNFY6c > div.VfPpkd-wzTsW.O4g5Md.iWO5td > div > div.VfPpkd-cnG4Wd.m5OsGf > div > div.VfPpkd-T0kwCb.IdSMxc > button.VfPpkd-LgbsSe.VfPpkd-LgbsSe-OWXEXe-k8QpJ.nCP5yc.AjY5Oe.LQeN7.kDryjd'
};

const delay = ms => new Promise(res => setTimeout(res, ms));

async function deletePhotos(idsToDelete) {
  const notDeleted = [];
  for (const id of idsToDelete) {
    const otherWindow = window.open(`${baseUrl}${id}`, '_blank');
    try {
      await new Promise(resolve => {
        otherWindow.onload = async () => {
          await delay(1000);
          const originalUrl = otherWindow.location.href;
          const deleteButton = otherWindow.document.querySelector(QUERY_SELECTORS.deleteButton);
          if (deleteButton === null) {            
            reject(`Could not find delete button for photo with id ${id}`);
            return;
          }
          deleteButton.click();
          await delay(200);
          const confirmationButton = otherWindow.document.querySelector(QUERY_SELECTORS.confirmationButton);
          confirmationButton.click();
          
          // Start a race between the while loop and a 5 second timer
          await Promise.race([
            new Promise(async (resolve) => {
              while (otherWindow.location.href === originalUrl) {
                await delay(100);
              }
              otherWindow.close();            
              resolve();
            }),
            // 5 seconds max try
            new Promise((_, reject) =>  setTimeout(() => reject(new Error('Deletion took too long')), 5000))
          ]);

          resolve();
        };
      });
    } catch (error) {
      console.error(`Error deleting photo with id ${id}: ${error}`);
      notDeleted.push(id);
    }    
  }
  return notDeleted
}

const notDeleted = deletePhotos(idsToDelete);
if (notDeleted.length) {
  console.error(`The following photos could not be deleted: ${notDeleted}`);
}
