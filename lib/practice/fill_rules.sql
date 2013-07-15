insert into rules (name, message, common) 
	values ('tagBody', 'You should add tag <body> to this page.', 'false');
insert into rules (name, message, common) 
	values ('tagHtml', 'You should add tag <html> to this page.', 'false');
insert into rules (name, message, common) 
	values ('tagHtml', 'You should add tag <head> to this page.', 'false');
insert into rules (name, message, common) 
	values ('tagTitle', 'You should add tag <title> to this page.', 'false');
insert into rules (name, message, common) 
	values ('inlineCss', 'You should remove inline CSS from this page.', 'false');
insert into rules (name, message, common) 
	values ('inlineJs', 'You should remove inline JS from this page.', 'false');
insert into rules (name, message) values ('robotsTxt', 'You should add file robots.txt to your project.');
insert into rules (name, message) values ('error404', 'You should customize page for displaying error 404.');
insert into rules (name, message) values ('redirect', 'You should handle redirect.');