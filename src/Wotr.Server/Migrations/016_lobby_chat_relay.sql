-- LOBBY-03: the Open games chat is relayed in the server's memory only (LobbyChat), never stored; the table of
-- migration 015 and the messages in it go.
DROP TABLE IF EXISTS lobby_messages;
