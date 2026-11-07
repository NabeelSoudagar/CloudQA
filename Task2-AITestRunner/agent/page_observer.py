from typing import List, Dict, Any
from selenium.webdriver.common.by import By

class PageObserver:
    @staticmethod
    def inspect(driver) -> Dict[str, Any]:
        """Extracts a structured snapshot of visible interactive elements on the page."""
        url = driver.current_url
        title = driver.title

        elements_snapshot: List[Dict[str, Any]] = []

        # Find visible inputs and textareas
        inputs = driver.find_elements(By.XPATH, "//input[not(@type='hidden')] | //textarea")
        for idx, el in enumerate(inputs):
            try:
                if not el.is_displayed():
                    continue
                tag = el.tag_name.lower()
                el_type = (el.get_attribute("type") or "text").lower()
                el_id = el.get_attribute("id") or ""
                el_name = el.get_attribute("name") or ""
                placeholder = el.get_attribute("placeholder") or ""
                aria = el.get_attribute("aria-label") or ""
                value = el.get_attribute("value") or ""

                # Look for associated label text
                label_text = ""
                if el_id:
                    labels = driver.find_elements(By.XPATH, f"//label[@for='{el_id}']")
                    if labels:
                        label_text = labels[0].text.strip()
                if not label_text:
                    try:
                        parent = el.find_element(By.XPATH, "..")
                        label_text = parent.text.strip().replace("\n", " ")[:60]
                    except Exception:
                        pass

                elements_snapshot.append({
                    "element_ref": el,
                    "index": len(elements_snapshot),
                    "tag": tag,
                    "type": el_type,
                    "id": el_id,
                    "name": el_name,
                    "placeholder": placeholder,
                    "aria": aria,
                    "label": label_text,
                    "current_value": value
                })
            except Exception:
                continue

        # Find visible buttons
        buttons = driver.find_elements(By.XPATH, "//button | //input[@type='submit' or @type='button']")
        for idx, btn in enumerate(buttons):
            try:
                if not btn.is_displayed():
                    continue
                tag = btn.tag_name.lower()
                btn_type = btn.get_attribute("type") or "button"
                btn_text = btn.text.strip().replace("\n", " ")
                btn_id = btn.get_attribute("id") or ""
                btn_name = btn.get_attribute("name") or ""
                btn_aria = btn.get_attribute("aria-label") or ""
                btn_value = btn.get_attribute("value") or ""

                display_name = btn_text or btn_aria or btn_value or btn_name or "Button"

                elements_snapshot.append({
                    "element_ref": btn,
                    "index": len(elements_snapshot),
                    "tag": tag,
                    "type": btn_type,
                    "id": btn_id,
                    "name": btn_name,
                    "label": display_name,
                    "text": btn_text
                })
            except Exception:
                continue

        # Find key visible links (navigation/action links)
        links = driver.find_elements(By.XPATH, "//a[@href and not(starts-with(@href, 'javascript:'))]")
        for lnk in links:
            try:
                if not lnk.is_displayed():
                    continue
                text = lnk.text.strip().replace("\n", " ")
                href = lnk.get_attribute("href") or ""
                if text and len(text) < 50:
                    elements_snapshot.append({
                        "element_ref": lnk,
                        "index": len(elements_snapshot),
                        "tag": "a",
                        "type": "link",
                        "id": lnk.get_attribute("id") or "",
                        "name": lnk.get_attribute("name") or "",
                        "label": text,
                        "href": href
                    })
            except Exception:
                continue

        return {
            "url": url,
            "title": title,
            "elements": elements_snapshot
        }

    @staticmethod
    def format_summary_for_llm(snapshot: Dict[str, Any]) -> str:
        """Formats the page snapshot into a concise textual table for the AI model."""
        lines = [
            f"Page URL: {snapshot['url']}",
            f"Page Title: {snapshot['title']}",
            "Interactive Elements on Screen:"
        ]
        for el in snapshot["elements"][:40]:  # Keep to top 40 relevant interactable controls
            ref = el["index"]
            tag = el["tag"]
            el_type = el.get("type", "")
            lbl = el.get("label", "") or el.get("placeholder", "") or el.get("text", "")
            name = el.get("name", "")
            lines.append(f"  [{ref}] <{tag} type='{el_type}' name='{name}'> Label/Text: '{lbl}'")

        return "\n".join(lines)

