import sys
from moxfield_scraper import Scraper


def check_url(args):
    if len(args) < 2:
        print("Usage: python ./main [URL]")
        exit()
    else:
        print(f"Moxfield URL received: {args[1]}")
        return str(args[1])


if __name__ == "__main__":
    url = check_url(sys.argv)
    Scraper().scrape_price(url)
