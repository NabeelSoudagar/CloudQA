import os
import time
from typing import Optional
from selenium import webdriver
from selenium.webdriver.chrome.options import Options
from selenium.webdriver.chrome.service import Service
from selenium.webdriver.common.by import By
from selenium.webdriver.support.ui import WebDriverWait
from selenium.webdriver.support import expected_conditions as EC
from selenium.common.exceptions import WebDriverException, ElementClickInterceptedException

class BrowserController:
    def __init__(self, headless: bool = False, timeout: int = 15, slow_mo_ms: int = 400):
        self.headless = headless
        self.timeout = timeout
        self.slow_mo_ms = slow_mo_ms
        self.driver: Optional[webdriver.Chrome] = None

    def start(self):
        opts = Options()
        if self.headless:
            opts.add_argument("--headless=new")
        opts.add_argument("--disable-gpu")
        opts.add_argument("--no-sandbox")
        opts.add_argument("--disable-dev-shm-usage")
        opts.add_argument("--window-size=1440,900")
        opts.add_argument("--start-maximized")
        opts.add_argument("--disable-blink-features=AutomationControlled")
        opts.add_experimental_option("excludeSwitches", ["enable-automation"])
        opts.add_experimental_option("useAutomationExtension", False)

        self.driver = webdriver.Chrome(options=opts)
        self.driver.implicitly_wait(3)
        self.driver.set_page_load_timeout(self.timeout)

    def navigate(self, url: str):
        if not self.driver:
            self.start()
        if not url.startswith("http://") and not url.startswith("https://"):
            url = "https://" + url
        self.driver.get(url)
        self.wait_for_ready()
        self.dismiss_common_overlays()

    def wait_for_ready(self, timeout: Optional[int] = None):
        t = timeout or self.timeout
        try:
            WebDriverWait(self.driver, t).until(
                lambda d: d.execute_script("return document.readyState") == "complete"
            )
        except Exception:
            pass
        time.sleep(0.5)

    def dismiss_common_overlays(self):
        """Dismisses common overlays such as cookie consent or demo disclaimers."""
        if not self.driver:
            return
        selectors = [
            "//button[@id='acknowledgeDemoWarning']",
            "//button[contains(translate(., 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'i understand')]",
            "//button[contains(translate(., 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'accept all')]",
            "//button[contains(translate(., 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'agree')]",
            "//button[contains(translate(., 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'close') and contains(@class, 'modal')]",
        ]
        for sel in selectors:
            try:
                elements = self.driver.find_elements(By.XPATH, sel)
                for el in elements:
                    if el.is_displayed():
                        self.driver.execute_script("arguments[0].click();", el)
                        time.sleep(0.4)
            except Exception:
                pass

    def capture_screenshot(self, filepath: str) -> str:
        if not self.driver:
            return ""
        os.makedirs(os.path.dirname(os.path.abspath(filepath)), exist_ok=True)
        self.driver.save_screenshot(filepath)
        return filepath

    def get_current_url(self) -> str:
        return self.driver.current_url if self.driver else ""

    def get_page_title(self) -> str:
        return self.driver.title if self.driver else ""

    def get_body_text(self) -> str:
        try:
            return self.driver.find_element(By.TAG_NAME, "body").text
        except Exception:
            return ""

    def scroll_into_view(self, element):
        try:
            self.driver.execute_script("arguments[0].scrollIntoView({behavior: 'smooth', block: 'center'});", element)
            time.sleep(0.2)
        except Exception:
            pass

    def highlight_element(self, element):
        try:
            self.driver.execute_script(
                "arguments[0].style.outline = '3px solid #3b82f6'; arguments[0].style.boxShadow = '0 0 10px rgba(59,130,246,0.8)';",
                element
            )
        except Exception:
            pass

    def click(self, element) -> bool:
        self.scroll_into_view(element)
        self.highlight_element(element)
        time.sleep(self.slow_mo_ms / 1000.0)
        try:
            element.click()
            self.wait_for_ready()
            self.dismiss_common_overlays()
            return True
        except (ElementClickInterceptedException, WebDriverException):
            try:
                self.driver.execute_script("arguments[0].click();", element)
                self.wait_for_ready()
                self.dismiss_common_overlays()
                return True
            except Exception:
                return False

    def type_text(self, element, text: str) -> bool:
        self.scroll_into_view(element)
        self.highlight_element(element)
        time.sleep(self.slow_mo_ms / 1000.0)
        try:
            element.click()
            element.clear()
            element.send_keys(text)
            return True
        except Exception:
            try:
                self.driver.execute_script(
                    "arguments[0].value = arguments[1]; arguments[0].dispatchEvent(new Event('input', {bubbles:true})); arguments[0].dispatchEvent(new Event('change', {bubbles:true}));",
                    element, text
                )
                return True
            except Exception:
                return False

    def close(self):
        if self.driver:
            try:
                self.driver.quit()
            except Exception:
                pass
            self.driver = None

