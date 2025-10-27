const playwright = require('playwright');

(async () => {
  const browser = await playwright.chromium.launch();
  const page = await browser.newPage();
  page.setViewportSize({ width: 1920, height: 1080 });
  
  await page.goto('http://localhost:5000/project-manager');
  await page.waitForTimeout(2000);
  
  await page.screenshot({ 
    path: '/home/runner/work/Minotaur/Minotaur/docs/ui-screenshots/17_Project_Manager_Phase2.png',
    fullPage: true
  });
  
  await browser.close();
})();
